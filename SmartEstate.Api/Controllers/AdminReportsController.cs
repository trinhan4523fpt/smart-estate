using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEstate.Shared.Time;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Time;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public sealed class AdminReportsController : ControllerBase
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AdminReportsController(SmartEstateDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("listings")]
    public async Task<IActionResult> GetListingReports([FromQuery] bool? isResolved, CancellationToken ct)
    {
        var q = _db.ListingReports.AsNoTracking().Where(x => !x.IsDeleted);
        if (isResolved is not null) q = q.Where(x => x.IsResolved == isResolved.Value);
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.ListingId,
                x.ReporterUserId,
                x.Reason,
                x.Detail,
                x.IsResolved,
                x.ResolvedAt,
                x.ResolvedByAdminId,
                x.ResolutionNote,
                x.CreatedAt
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    public sealed record ResolveListingReportRequest(string ResolutionNote);

    [HttpPost("listings/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveListingReport([FromRoute] Guid id, [FromBody] ResolveListingReportRequest req, CancellationToken ct)
    {
        var adminId = _currentUser.UserId;
        if (adminId is null) return Unauthorized(new AppError(ErrorCodes.Unauthorized, "Unauthorized."));

        var report = await _db.ListingReports.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (report is null) return NotFound(new AppError(ErrorCodes.NotFound, "Report not found."));
        if (report.IsResolved) return Ok();

        report.IsResolved = true;
        report.ResolvedAt = DateTimeOffset.UtcNow;
        report.ResolvedByAdminId = adminId.Value;
        report.ResolutionNote = string.IsNullOrWhiteSpace(req.ResolutionNote) ? null : req.ResolutionNote.Trim();
        await _db.SaveChangesAsync(true, ct);
        return Ok();
    }
}
