using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.BrokerTakeover;
using SmartEstate.App.Features.BrokerTakeover.Dtos;
using SmartEstate.Shared.Errors;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/takeovers")]
public sealed class TakeoversController : ControllerBase
{
    private readonly TakeoverService _svc;

    public TakeoversController(TakeoverService svc)
    {
        _svc = svc;
    }

    // Seller requests takeover
    /// <summary>
    /// Seller requests a broker takeover for a listing.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(typeof(Guid), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> RequestTakeover([FromBody] RequestTakeoverRequest req, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.RequestAsync(req, isAdmin, ct);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    // Broker accepts/rejects
    /// <summary>
    /// Broker accepts or rejects a takeover request.
    /// </summary>
    [HttpPost("{id:guid}/decide")]
    [Authorize(Roles = "Broker,Admin")]
    [ProducesResponseType(typeof(Guid), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Decide([FromRoute] Guid id, [FromBody] DecideTakeoverRequest req, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.DecideAsync(id, req.Accept, isAdmin, ct);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }


    // Seller unassign broker
    /// <summary>
    /// Seller unassigns a broker from a listing.
    /// </summary>
    [HttpPost("/api/listings/{listingId:guid}/unassign-broker")]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UnassignBroker([FromRoute] Guid listingId, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.UnassignBrokerAsync(listingId, isAdmin, ct);
        return result.IsSuccess ? Ok() : MapError(result.Error);
    }

    private IActionResult MapError(AppError? e)
    {
        if (e is null) return StatusCode(500, new AppError(ErrorCodes.Unexpected, "Unexpected error"));

        return e.Code switch
        {
            ErrorCodes.Validation => BadRequest(e),
            ErrorCodes.Unauthorized => Unauthorized(e),
            ErrorCodes.Forbidden => Forbid(),
            ErrorCodes.NotFound => NotFound(e),
            ErrorCodes.Conflict => Conflict(e),
            _ => StatusCode(500, e)
        };
    }
}
