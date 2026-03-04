using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Points.Dtos;
using SmartEstate.Domain.Entities;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.App.Features.Points;

public sealed class PointTransactionService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public PointTransactionService(
        SmartEstateDbContext db,
        ICurrentUser currentUser,
        IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
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

    public async Task<Result<PointPaymentResponse>> CreatePaymentAsync(CreatePointPaymentRequest req, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<PointPaymentResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var pkg = await _db.PointPackages.FirstOrDefaultAsync(x => x.Id == req.PointPackageId && !x.IsDeleted && x.IsActive, ct);
        if (pkg is null) return Result<PointPaymentResponse>.Fail(ErrorCodes.NotFound, "Package not found.");

        var transaction = new PointTransaction
        {
            UserId = userId.Value,
            PackageId = pkg.Id,
            Points = pkg.Points,
            Amount = pkg.PriceAmount,
            Currency = pkg.PriceCurrency,
            Status = PointTransactionStatus.Pending
        };

        _db.PointTransactions.Add(transaction);
        await _db.SaveChangesAsync(true, ct);

        var payment = Payment.Create(
            payerUserId: userId.Value,
            amount: pkg.PriceAmount,
            currency: pkg.PriceCurrency,
            feeType: FeeType.PointPurchase,
            refType: RefType.PointTransaction,
            refId: transaction.Id,
            description: $"Buy {pkg.Points} points ({pkg.Name})",
            provider: "VNPay"
        );

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(true, ct);

        transaction.PaymentId = payment.Id;
        await _db.SaveChangesAsync(true, ct);

        var payUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?token={payment.Id}";
        
        return Result<PointPaymentResponse>.Ok(new PointPaymentResponse(payUrl));
    }

    public async Task<Result<List<PointTransactionHistoryResponse>>> GetHistoryAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<List<PointTransactionHistoryResponse>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var history = await _db.PointTransactions
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Include(x => x.PointPackage)
            .ToListAsync(ct);

        var dtos = history.Select(x => new PointTransactionHistoryResponse(
            x.Id,
            x.PointPackage.Name,
            x.Points,
            x.Amount,
            x.Status.ToString(),
            x.CreatedAt
        )).ToList();

        return Result<List<PointTransactionHistoryResponse>>.Ok(dtos);
    }
}
