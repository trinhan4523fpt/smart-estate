using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.Shared.Time;
using SmartEstate.Domain.Entities;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/listings/{listingId:guid}/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ReportsController(SmartEstateDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public sealed record CreateListingReportRequest(string Reason, string? Detail);

    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(AppError), 400)]
    [ProducesResponseType(typeof(AppError), 401)]
    [ProducesResponseType(typeof(AppError), 404)]
    public async Task<IActionResult> Create([FromRoute] Guid listingId, [FromBody] CreateListingReportRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new AppError(ErrorCodes.Unauthorized, "Unauthorized."));

        var listing = await _db.Listings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (listing is null) return NotFound(new AppError(ErrorCodes.NotFound, "Listing not found."));

        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new AppError(ErrorCodes.Validation, "Reason is required."));

        var report = new ListingReport
        {
            ListingId = listingId,
            ReporterUserId = userId.Value,
            Reason = req.Reason.Trim(),
            Detail = string.IsNullOrWhiteSpace(req.Detail) ? null : req.Detail.Trim(),
            IsResolved = false
        };

        _db.ListingReports.Add(report);
        await _db.SaveChangesAsync(true, ct);
        return Ok(new { report.Id });
    }
}
