using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Points.Dtos;
using SmartEstate.Domain.Entities;
 
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;
using SmartEstate.Domain.Enums;

namespace SmartEstate.App.Features.Points;

public sealed class PointPurchaseService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IPaymentGateway _payments;
    private readonly PointsService _points;

    public PointPurchaseService(
        SmartEstateDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        IPaymentGateway payments,
        PointsService points)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _payments = payments;
        _points = points;
    }

    public async Task<Result<List<PointPackageDto>>> GetPackagesAsync(CancellationToken ct = default)
    {
        var pkgs = await _db.PointPackages
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.PriceAmount)
            .ToListAsync(ct);

        var dtos = pkgs
            .Select(x => new PointPackageDto(x.Id, x.Name, x.Points, x.PriceAmount, x.PriceCurrency))
            .ToList();

        return Result<List<PointPackageDto>>.Ok(dtos);
    }

    public async Task<Result<PointPurchaseInitResponse>> CreatePurchaseAsync(CreatePointPurchaseRequest req, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<PointPurchaseInitResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var pkg = await _db.PointPackages.FirstOrDefaultAsync(x => x.Id == req.PointPackageId && !x.IsDeleted && x.IsActive, ct);
        if (pkg is null) return Result<PointPurchaseInitResponse>.Fail(ErrorCodes.NotFound, "Package not found.");

        var purchase = new PointPurchase
        {
            UserId = userId.Value,
            PointPackageId = pkg.Id,
            Points = pkg.Points,
            PriceAmount = pkg.PriceAmount,
            PriceCurrency = pkg.PriceCurrency,
            Status = PointPurchaseStatus.Pending
        };

        _db.PointPurchases.Add(purchase);
        await _db.SaveChangesAsync(true, ct);

        var payInit = await _payments.CreatePaymentAsync(
            payerUserId: userId.Value,
            amount: pkg.PriceAmount,
            currency: pkg.PriceCurrency,
            description: $"Point package {pkg.Name}",
            ct: ct);

        var payment = Payment.CreatePointPurchasePayment(
            payerUserId: userId.Value,
            pointPurchaseId: purchase.Id,
            amount: pkg.PriceAmount,
            currency: pkg.PriceCurrency,
            provider: payInit.Provider,
            providerRef: payInit.ProviderRef,
            payUrl: payInit.PayUrl);

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(true, ct);

        purchase.PaymentId = payment.Id;
        await _db.SaveChangesAsync(true, ct);

        return Result<PointPurchaseInitResponse>.Ok(
            new PointPurchaseInitResponse(purchase.Id, payment.Id, payInit.Provider, payInit.PayUrl));
    }

    public async Task<Result> MarkPaymentPaidAsync(Guid paymentId, string? rawPayloadJson, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(x => x.Id == paymentId && !x.IsDeleted, ct);
        if (payment is null) return Result.Fail(ErrorCodes.NotFound, "Payment not found.");

        if (payment.Status == PaymentStatus.Paid) return Result.Ok();

        payment.MarkPaid(rawPayloadJson);

        if (payment.PointPurchaseId is null)
            return Result.Fail(ErrorCodes.Validation, "Payment is not linked to a point purchase.");

        var purchase = await _db.PointPurchases.FirstOrDefaultAsync(x => x.Id == payment.PointPurchaseId.Value && !x.IsDeleted, ct);
        if (purchase is null) return Result.Fail(ErrorCodes.NotFound, "Point purchase not found.");

        if (purchase.Status == PointPurchaseStatus.Completed) return Result.Ok();

        var r = await _points.AddPermanentAsync(
            purchase.UserId,
            purchase.Points,
            "PURCHASE_POINTS",
            "PointPurchase",
            purchase.Id,
            ct);

        if (!r.IsSuccess) return r;

        purchase.Status = PointPurchaseStatus.Completed;
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }
}
