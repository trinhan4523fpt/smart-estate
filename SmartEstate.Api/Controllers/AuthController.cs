using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Auth;
using SmartEstate.App.Features.Auth.Dtos;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Register a new user (Buyer, Seller, Broker).
    /// </summary>
    /// <response code="204">Registration successful.</response>
    /// <response code="409">Email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(SmartEstate.Shared.Errors.AppError), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req, ct);
        if (!result.IsSuccess)
        {
            // Map specific errors if needed
            if (result.Error!.Code == SmartEstate.Shared.Errors.ErrorCodes.Conflict)
                return Conflict(result.Error);
                
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Login with Email and Password.
    /// </summary>
    /// <response code="200">Login successful, returns JWT token.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(SmartEstate.Shared.Errors.AppError), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);
        if (!result.IsSuccess)
            return Unauthorized(result.Error);

        return Ok(result.Value);
    }
}
