using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.BrokerRequests;
using SmartEstate.App.Features.BrokerRequests.Dtos;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/broker-requests")]
public sealed class BrokerRequestsController : ControllerBase
{
    private readonly BrokerRequestService _svc;

    public BrokerRequestsController(BrokerRequestService svc)
    {
        _svc = svc;
    }

    [HttpPost("/api/listings/{listingId:guid}/takeover")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> Create([FromRoute] Guid listingId, [FromBody] CreateBrokerRequestRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(listingId, req.BrokerId, ct);
        return ToActionResult(result, created: true);
    }

    [HttpPost("takeover/request")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> CreateFromPayload([FromBody] CreateBrokerRequestPayload req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(req.ListingId, req.BrokerId, ct);
        return ToActionResult(result, created: true);
    }

    [HttpGet("takeover/requests")]
    [Authorize]
    public async Task<IActionResult> GetMyRequests(CancellationToken ct)
    {
        var result = await _svc.GetMyRequestsAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("managed-listings")]
    [Authorize(Roles = "Broker,Admin")]
    public async Task<IActionResult> GetManagedListings(CancellationToken ct)
    {
        var result = await _svc.GetManagedListingsAsync(ct);
        return ToActionResult(result);
    }

    [HttpPatch("takeover/{id:guid}")]
    [Authorize(Roles = "Broker,Admin")]
    public async Task<IActionResult> Respond([FromRoute] Guid id, [FromBody] RespondBrokerRequestRequest req, CancellationToken ct)
    {
        var result = await _svc.RespondAsync(id, req.Status, ct);
        return ToActionResult(result);
    }

    [HttpDelete("takeover/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.DeleteAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpGet("sent")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetSent(CancellationToken ct)
    {
        var result = await _svc.GetSentRequestsAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("received")]
    [Authorize(Roles = "Broker,Admin")]
    public async Task<IActionResult> GetReceived(CancellationToken ct)
    {
        var result = await _svc.GetReceivedRequestsAsync(ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/accept")]
    [Authorize(Roles = "Broker,Admin")]
    public async Task<IActionResult> Accept([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.AcceptAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Broker,Admin")]
    public async Task<IActionResult> Reject([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.RejectAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> ConfirmPayment([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.PayFeeAsync(id, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/payment")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> Pay([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.PayFeeAsync(id, ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess) return Ok();
        return ErrorResult(result.Error);
    }

    private IActionResult ToActionResult<T>(Result<T> result, bool created = false)
    {
        if (result.IsSuccess) return created ? StatusCode(201, result.Value) : Ok(result.Value);
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
