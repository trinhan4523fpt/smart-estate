using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Auth;
using SmartEstate.App.Features.Auth.Dtos;
using SmartEstate.Shared.Errors;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly SmartEstate.App.Common.Abstractions.ICurrentUser _currentUser;

    public UsersController(AuthService auth, SmartEstate.App.Common.Abstractions.ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await _auth.GetMyProfileAsync(userId.Value, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await _auth.UpdateMyProfileAsync(userId.Value, req, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        
        return NoContent();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetUsers([FromQuery] string? role, CancellationToken ct)
    {
        // This is a simple implementation. In real app, we might restrict who can see user list.
        // Frontend uses it to fetch all brokers.
        
        var result = await _auth.GetUsersAsync(role, ct);
        return Ok(result.Value);
    }
}
