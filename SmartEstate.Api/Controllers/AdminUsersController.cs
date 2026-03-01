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
            .Select(x => new { x.Id, x.Email, x.DisplayName, x.Phone, x.RoleId, Role = x.Role.Name, x.IsActive, x.LastLoginAt })
            .OrderBy(x => x.Email)
            .ToListAsync(ct);
        return Ok(users);
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
