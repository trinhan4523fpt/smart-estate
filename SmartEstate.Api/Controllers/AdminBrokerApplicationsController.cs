using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.BrokerApplications;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/admin/broker-applications")]
[Authorize(Roles = "Admin")]
public sealed class AdminBrokerApplicationsController : ControllerBase
{
    private readonly BrokerApplicationService _svc;

    public AdminBrokerApplicationsController(BrokerApplicationService svc)
    {
        _svc = svc;
    }

    [HttpPut("{id:guid}/approve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(AppError), 400)]
    [ProducesResponseType(typeof(AppError), 401)]
    [ProducesResponseType(typeof(AppError), 404)]
    public async Task<IActionResult> Approve([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.AdminApproveAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/reject")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(AppError), 400)]
    [ProducesResponseType(typeof(AppError), 401)]
    [ProducesResponseType(typeof(AppError), 404)]
    public async Task<IActionResult> Reject([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.AdminRejectAsync(id, ct);
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
}

