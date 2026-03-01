﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Listings;
using SmartEstate.App.Features.Listings.Dtos;
using SmartEstate.Domain.Enums;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/listings")]
public sealed class ListingsController : ControllerBase
{
    private readonly ListingService _svc;
    private readonly ICurrentUser _currentUser;

    public sealed class ListingUploadImageRequest
    {
        public IFormFile File { get; set; } = default!;
        public int SortOrder { get; set; } = 0;
        public string? Caption { get; set; }
    }
    public ListingsController(ListingService svc, ICurrentUser currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    // Create listing: User/Broker/Admin
    /// <summary>
    /// Create a new property listing.
    /// </summary>
    /// <response code="201">Listing created successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">User is not Seller or Broker.</response>
    [HttpPost]
    [Authorize(Roles = "User,Broker,Admin")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(req, ct);
        return ToActionResult(result, created: true);
    }

    // Update listing: responsible or admin
    /// <summary>
    /// Update an existing listing.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "User,Broker,Admin")]
    [ProducesResponseType(typeof(Guid), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateListingRequest req, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.UpdateAsync(id, req, isAdmin, ct);
        return ToActionResult(result);
    }

    // Get listing detail:
    // - public only: APPROVED + ACTIVE
    // - owner/admin can view non-public
    /// <summary>
    /// Get listing details.
    /// </summary>
    /// <remarks>
    /// Public users can only see APPROVED and ACTIVE listings.
    /// Phone number is masked unless "Get Contact" API is used.
    /// </remarks>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
<<<<<<< Updated upstream
    [ProducesResponseType(typeof(SmartEstate.App.Features.Listings.Dtos.ListingDetailResponse), 200)]
=======
    [ProducesResponseType(typeof(ListingDetailResponse), 200)]
>>>>>>> Stashed changes
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDetail([FromRoute] Guid id, CancellationToken ct)
    {
        var viewerId = _currentUser.UserId;
        var isAdmin = User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
        var result = await _svc.GetDetailAsync(id, viewerId, isAdmin, ct);
        return ToActionResult(result);
    }

    // Get contact info (real phone) - require login
    /// <summary>
    /// Reveal the contact phone number of the listing owner.
    /// </summary>
    /// <remarks>Requires authentication.</remarks>
    [HttpPost("{id:guid}/contact")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetContact([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _svc.GetContactAsync(id, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { Phone = result.Value });
    }

    // Update lifecycle: responsible or admin
    [HttpPatch("{id:guid}/lifecycle")]
    [Authorize(Roles = "User,Broker,Admin")]
    public async Task<IActionResult> UpdateLifecycle([FromRoute] Guid id, [FromBody] UpdateLifecycleRequest req, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await _svc.UpdateLifecycleAsync(id, req.Status, isAdmin, ct);
        return ToActionResult(result);
    }

    // Upload image: multipart/form-data
    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "User,Broker,Admin")]
    [RequestSizeLimit(25_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(
    [FromRoute] Guid id,
    [FromForm] ListingUploadImageRequest req,
    CancellationToken ct = default)
    {
        if (req.File is null || req.File.Length == 0)
            return BadRequest(new AppError(ErrorCodes.Validation, "File is required."));

        var isAdmin = User.IsInRole("Admin");

        await using var stream = req.File.OpenReadStream();
        var result = await _svc.UploadImageAsync(
            listingId: id,
            content: stream,
            fileName: req.File.FileName,
            contentType: req.File.ContentType,
            sortOrder: req.SortOrder,
            caption: req.Caption,
            isAdmin: isAdmin,
            ct: ct);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result, bool created = false)
    {
        if (result.IsSuccess) return created ? StatusCode(201) : Ok();

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

    private IActionResult ToActionResult<T>(Result<T> result, bool created = false)
    {
        if (result.IsSuccess) return created ? StatusCode(201, result.Value) : Ok(result.Value);

        return result.Error?.Code switch
        {
            ErrorCodes.Validation => BadRequest(result.Error),
            ErrorCodes.Unauthorized => Unauthorized(result.Error),
            ErrorCodes.Forbidden => StatusCode(403, result.Error),
            ErrorCodes.NotFound => StatusCode(404, result.Error),
            ErrorCodes.Conflict => StatusCode(409, result.Error),
            _ => StatusCode(500, result.Error ?? new AppError(ErrorCodes.Unexpected, "Unexpected error"))
        };
    }
}
