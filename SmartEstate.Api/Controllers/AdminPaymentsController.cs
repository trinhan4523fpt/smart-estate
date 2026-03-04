using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Reports;
using SmartEstate.App.Features.Reports.Dtos;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "Admin")]
public sealed class AdminPaymentsController : ControllerBase
{
    private readonly PaymentReportingService _svc;

    public AdminPaymentsController(PaymentReportingService svc)
    {
        _svc = svc;
    }

    [HttpGet("point-purchases")]
    public async Task<IActionResult> GetPointPurchaseTotals(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var result = await _svc.GetPointPurchaseTotalsAsync(
            from ?? DateTimeOffset.UtcNow.AddDays(-30),
            to ?? DateTimeOffset.UtcNow,
            ct);

        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments(CancellationToken ct)
    {
        var result = await _svc.GetPaymentsAsync(ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var result = await _svc.GetPaymentsAsync(ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }
}

