﻿using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Auth;
using SmartEstate.App.Features.Auth.Dtos;
using SmartEstate.Shared.Errors;

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
    /// <response code="200">Registration successful, returns JWT token.</response>
    /// <response code="409">Email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
<<<<<<< Updated upstream
    [ProducesResponseType(typeof(SmartEstate.Shared.Errors.AppError), 409)]
=======
    [ProducesResponseType(typeof(AppError), 409)]
>>>>>>> Stashed changes
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req, ct);
        if (!result.IsSuccess)
            return Conflict(result.Error); // email exists => 409

        return Ok(result.Value);
    }

    /// <summary>
    /// Login with Email and Password.
    /// </summary>
    /// <response code="200">Login successful, returns JWT token.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
<<<<<<< Updated upstream
    [ProducesResponseType(typeof(SmartEstate.Shared.Errors.AppError), 401)]
=======
    [ProducesResponseType(typeof(AppError), 401)]
>>>>>>> Stashed changes
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);
        if (!result.IsSuccess)
            return Unauthorized(result.Error);

        return Ok(result.Value);
    }
}
