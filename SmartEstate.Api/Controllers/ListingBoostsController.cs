using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.ListingBoosts;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/listings")]
public sealed class ListingBoostsController : ControllerBase
{
    private readonly ListingBoostService _svc;

    public ListingBoostsController(ListingBoostService svc)
    {
        _svc = svc;
    }

    [HttpPost("{id:guid}/boost")]
<<<<<<< Updated upstream
    [Authorize(Roles = "User,Broker,Admin")]
=======
    [Authorize(Roles = "Seller,Broker,Admin")]
>>>>>>> Stashed changes
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(AppError), 400)]
    [ProducesResponseType(typeof(AppError), 401)]
    [ProducesResponseType(typeof(AppError), 403)]
    [ProducesResponseType(typeof(AppError), 409)]
    [ProducesResponseType(typeof(AppError), 404)]
    public async Task<IActionResult> Boost([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.BoostAsync(id, ct);
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
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
