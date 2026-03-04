using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Listings;
using SmartEstate.App.Features.Listings.Dtos;
using SmartEstate.Domain.Enums;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/listings")]
public sealed class ListingsController : ControllerBase
{
    private readonly ListingService _svc;

    public ListingsController(ListingService svc)
    {
        _svc = svc;
    }

    [HttpGet("/api/users/me/listings")]
    [Authorize]
    public async Task<IActionResult> GetMyListings(CancellationToken ct)
    {
        var result = await _svc.GetMyListingsAsync(ct);
        return ToActionResult(result);
    }

    [HttpGet("/api/admin/listings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminListings(CancellationToken ct)
    {
        var result = await _svc.GetAdminListingsAsync(ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Seller,Broker,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(req, ct);
        return ToActionResult(result, created: true);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Seller,Broker,Admin")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateListingRequest req, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.UpdateAsync(id, req, isAdmin, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Seller,Broker,Admin")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.DeleteAsync(id, isAdmin, ct);
        if (!result.IsSuccess) return ToActionResult(result);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetail([FromRoute] Guid id, CancellationToken ct)
    {
        var isAdmin = User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
        Guid? viewerId = null;
        if (User?.Identity?.IsAuthenticated == true)
        {
             var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
             if (Guid.TryParse(sub, out var g)) viewerId = g;
        }

        var result = await _svc.GetDetailAsync(id, viewerId, isAdmin, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/contact")]
    [Authorize]
    public async Task<IActionResult> GetContact([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.GetContactAsync(id, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { Phone = result.Value });
    }

    [HttpPatch("{id:guid}/submit")]
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Seller,Broker,Admin")]
    public async Task<IActionResult> Submit([FromRoute] Guid id, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.SubmitAsync(id, isAdmin, ct);
        if (!result.IsSuccess) return ToActionResult(result);
        
        return await GetDetail(id, ct);
    }

    [HttpPatch("{id:guid}/done")]
    [HttpPost("{id:guid}/done")]
    [Authorize(Roles = "Seller,Broker,Admin")]
    public async Task<IActionResult> Done([FromRoute] Guid id, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.UpdateLifecycleAsync(id, ListingLifecycleStatus.Done, isAdmin, ct);
        if (!result.IsSuccess) return ToActionResult(result);
        return await GetDetail(id, ct);
    }

    [HttpPatch("{id:guid}/cancel")]
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Seller,Broker,Admin")]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.UpdateLifecycleAsync(id, ListingLifecycleStatus.Cancelled, isAdmin, ct);
        if (!result.IsSuccess) return ToActionResult(result);
        return await GetDetail(id, ct);
    }

    [HttpPost("{id:guid}/report")]
    [Authorize]
    public async Task<IActionResult> Report([FromRoute] Guid id, [FromBody] ReportListingRequest req, CancellationToken ct)
    {
        var result = await _svc.ReportAsync(id, req.Reason, req.Note, ct);
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

    private IActionResult ErrorResult(SmartEstate.Shared.Errors.AppError? error)
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
