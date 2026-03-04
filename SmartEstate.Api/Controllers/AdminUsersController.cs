using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly SmartEstateDbContext _db;
    public AdminUsersController(SmartEstateDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking().Where(x => !x.IsDeleted);
        if (isActive is not null) q = q.Where(x => x.IsActive == isActive.Value);
        
        var users = await q
            .OrderBy(x => x.Email)
            .Select(x => new { 
                id = x.Id, 
                name = x.DisplayName, 
                email = x.Email, 
                role = x.Role.ToString().ToLower(), 
                profile = new {
                    avatar = x.Avatar ?? "",
                    phone = x.Phone,
                    address = x.Address
                },
                isActive = x.IsActive,
                createdAt = x.CreatedAt,
                updatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);
            
        return Ok(users);
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole([FromRoute] Guid id, [FromBody] UpdateRoleRequest req, CancellationToken ct)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (u is null) return NotFound(new AppError(ErrorCodes.NotFound, "User not found."));
        
        // Frontend sends "admin", "broker", "user" (lowercase) or capitalized.
        // We should handle case-insensitive parsing.
        
        if (Enum.TryParse<SmartEstate.Domain.Enums.UserRole>(req.Role, true, out var role))
        {
            u.SetRole(role);
            await _db.SaveChangesAsync(true, ct);
            return Ok(new { 
                id = u.Id, 
                name = u.DisplayName, 
                email = u.Email, 
                role = u.Role.ToString().ToLower(), 
                profile = new {
                    avatar = u.Avatar ?? "",
                    phone = u.Phone,
                    address = u.Address
                },
                isActive = u.IsActive,
                createdAt = u.CreatedAt,
                updatedAt = u.UpdatedAt
            });
        }
        
        return BadRequest(new AppError(ErrorCodes.Validation, "Invalid role."));
    }

    public record UpdateRoleRequest(string Role);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (u is null) return NotFound(new AppError(ErrorCodes.NotFound, "User not found."));
        
        _db.Users.Remove(u);
        await _db.SaveChangesAsync(true, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive([FromRoute] Guid id, [FromQuery] bool isActive, CancellationToken ct)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (u is null) return NotFound(new AppError(ErrorCodes.NotFound, "User not found."));
        if (isActive) u.Activate(); else u.Deactivate();
        await _db.SaveChangesAsync(true, ct);
        return Ok();
    }
}
