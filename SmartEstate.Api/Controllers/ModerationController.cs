using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Moderation;
using SmartEstate.App.Features.Moderation.Dtos;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/admin/moderation")]
[Authorize(Roles = "Admin")]
public sealed class ModerationController : ControllerBase
{
    private readonly ModerationService _svc;

    public ModerationController(ModerationService svc)
    {
        _svc = svc;
    }

    [HttpGet("listings/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingListingModerationItemDto>), 200)]
    public async Task<IActionResult> GetPendingListings(CancellationToken ct)
    {
        var result = await _svc.GetPendingListingsAsync(ct);
        return ToActionResult(result);
    }

    public sealed class ApproveListingRequest
    {
        public string? Reason { get; set; }
    }

    public sealed class RejectListingRequest
    {
        public string Reason { get; set; } = default!;
    }

   
    [HttpPut("/api/admin/listings/{id:guid}/approve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(AppError), 400)]
    [ProducesResponseType(typeof(AppError), 401)]
    [ProducesResponseType(typeof(AppError), 404)]
    public async Task<IActionResult> ApproveListing([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.ApproveAsync(id, ct);
        return ToActionResult(result);
    }

 
    [HttpPut("/api/admin/listings/{id:guid}/reject")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(AppError), 400)]
    [ProducesResponseType(typeof(AppError), 401)]
    [ProducesResponseType(typeof(AppError), 404)]
    public async Task<IActionResult> RejectListing([FromRoute] Guid id, [FromBody] RejectListingRequest req, CancellationToken ct)
    {
        var result = await _svc.RejectAsync(id, req.Reason, ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess) return Ok();

        return result.Error?.Code switch
        {
            ErrorCodes.Validation => BadRequest(result.Error),
            ErrorCodes.Unauthorized => Unauthorized(result.Error),
            ErrorCodes.Forbidden => Forbid(),
            ErrorCodes.NotFound => NotFound(result.Error),
            ErrorCodes.Conflict => Conflict(result.Error),
            _ => StatusCode(500, result.Error ?? new AppError(ErrorCodes.Unexpected, "Unexpected error"))
        };
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);

        return result.Error?.Code switch
        {
            ErrorCodes.Validation => BadRequest(result.Error),
            ErrorCodes.Unauthorized => Unauthorized(result.Error),
            ErrorCodes.Forbidden => Forbid(),
            ErrorCodes.NotFound => NotFound(result.Error),
            ErrorCodes.Conflict => Conflict(result.Error),
            _ => StatusCode(500, result.Error ?? new AppError(ErrorCodes.Unexpected, "Unexpected error"))
        };
    }
}

