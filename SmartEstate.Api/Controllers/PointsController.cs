using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Points;
using SmartEstate.App.Features.Points.Dtos;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/points")]
[Authorize]
public sealed class PointsController : ControllerBase
{
    private readonly PointTransactionService _transactions;
    private readonly PointsService _points;

    public PointsController(PointTransactionService transactions, PointsService points)
    {
        _transactions = transactions;
        _points = points;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var result = await _points.GetBalanceAsync(ct);
        return ToActionResult(result);
    }
    
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var result = await _transactions.GetHistoryAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("packages")]
    public async Task<IActionResult> GetPackages(CancellationToken ct)
    {
        var result = await _transactions.GetPackagesAsync(ct);
        return ToActionResult(result);
    }

    [HttpPost("payments")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePointPaymentRequest req, CancellationToken ct)
    {
        var result = await _transactions.CreatePaymentAsync(req, ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        return ErrorResult(result.Error);
    }

    private IActionResult ErrorResult(AppError? error)
    {
        return error?.Code switch
        {
            ErrorCodes.Validation => BadRequest(error),
            ErrorCodes.Unauthorized => Unauthorized(error),
            ErrorCodes.Forbidden => Forbid(),
            ErrorCodes.NotFound => NotFound(error),
            ErrorCodes.Conflict => Conflict(error),
            _ => StatusCode(500, error ?? new AppError(ErrorCodes.Unexpected, "Unexpected error"))
        };
    }
}
