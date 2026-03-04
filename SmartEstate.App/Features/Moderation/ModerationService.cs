using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Moderation.Dtos;
using SmartEstate.Domain.Entities;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;
using SmartEstate.App.Features.Points;

namespace SmartEstate.App.Features.Moderation;

public sealed class ModerationService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly PointsService _points;

    public ModerationService(
        SmartEstateDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        PointsService points)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _points = points;
    }

    public async Task<Result<IReadOnlyList<PendingListingModerationItemDto>>> GetPendingListingsAsync(CancellationToken ct = default)
    {
        var items = await _db.Listings
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.ModerationStatus == ModerationStatus.PendingReview
                && x.LifecycleStatus == ListingLifecycleStatus.Active)
            .Select(x => new
            {
                Listing = x,
                LatestReport = x.ModerationReports
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.Listing.CreatedAt)
            .ToListAsync(ct);

        var result = items
            .Select(x =>
            {
                ModerationReportDto? reportDto = null;
                if (x.LatestReport is not null)
                {
                    var r = x.LatestReport;
                    reportDto = new ModerationReportDto(
                        r.Id,
                        r.Score,
                        r.Decision,
                        r.FlagsJson,
                        r.SuggestionsJson,
                        r.ReviewedByAdminId,
                        r.ReviewedAt,
                        r.ReviewOutcome);
                }

                var l = x.Listing;

                return new PendingListingModerationItemDto(
                    l.Id,
                    l.Title,
                    l.City,
                    l.District,
                    l.ModerationStatus,
                    l.LifecycleStatus,
                    l.CreatedAt,
                    reportDto);
            })
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<PendingListingModerationItemDto>>.Ok(result);
    }

    public async Task<Result> ApproveAsync(Guid listingId, CancellationToken ct = default)
    {
        var adminId = _currentUser.UserId;
        if (adminId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings
            .Include(x => x.ModerationReports)
            .FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);

        if (listing is null) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        listing.Approve();

        var latestReport = listing.ModerationReports
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (latestReport is null)
        {
            latestReport = ModerationReport.CreateFromAiDecision(
                listing.Id,
                listing.QualityScore,
                "NEED_REVIEW",
                null,
                listing.AiFlagsJson);

            _db.ModerationReports.Add(latestReport);
        }

        latestReport.MarkReviewed(adminId.Value, _clock.UtcNow, ModerationStatus.Approved);

        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result> RejectAsync(Guid listingId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Fail(ErrorCodes.Validation, "Reason is required.");

        var adminId = _currentUser.UserId;
        if (adminId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings
            .Include(x => x.ModerationReports)
            .FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);

        if (listing is null) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        listing.Reject(reason.Trim());
        // Hoàn điểm nếu bài đã chiếm điểm trước đó
        await _points.AddPermanentAsync(listing.CreatedByUserId, 1, "REFUND_POST", "Listing", listing.Id, ct);

        var latestReport = listing.ModerationReports
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (latestReport is null)
        {
            latestReport = ModerationReport.CreateFromAiDecision(
                listing.Id,
                listing.QualityScore,
                "NEED_REVIEW",
                reason,
                listing.AiFlagsJson);

            _db.ModerationReports.Add(latestReport);
        }

        latestReport.MarkReviewed(adminId.Value, _clock.UtcNow, ModerationStatus.Rejected);

        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }
}
