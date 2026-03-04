using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Reports;
using SmartEstate.Shared.Errors;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly PaymentReportingService _svc;

    public AdminDashboardController(PaymentReportingService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _svc.GetDashboardStatsAsync(ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }
}
