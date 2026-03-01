using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEstate.Domain.Entities;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/admin/point-packages")]
[Authorize(Roles = "Admin")]
public sealed class AdminPointPackagesController : ControllerBase
{
    private readonly SmartEstateDbContext _db;

    public AdminPointPackagesController(SmartEstateDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _db.PointPackages.AsNoTracking().OrderBy(x => x.PriceAmount).ToListAsync(ct);
        return Ok(items);
    }

    public sealed record UpsertRequest(string Name, int Points, decimal PriceAmount, string PriceCurrency, bool IsActive);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertRequest req, CancellationToken ct)
    {
        var p = new PointPackage
        {
            Name = req.Name,
            Points = req.Points,
            PriceAmount = req.PriceAmount,
            PriceCurrency = req.PriceCurrency,
            IsActive = req.IsActive
        };
        _db.PointPackages.Add(p);
        await _db.SaveChangesAsync(true, ct);
        return Ok(p);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpsertRequest req, CancellationToken ct)
    {
        var p = await _db.PointPackages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (p is null) return NotFound(new AppError(ErrorCodes.NotFound, "Package not found."));
        p.Name = req.Name;
        p.Points = req.Points;
        p.PriceAmount = req.PriceAmount;
        p.PriceCurrency = req.PriceCurrency;
        p.IsActive = req.IsActive;
        await _db.SaveChangesAsync(true, ct);
        return Ok(p);
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive([FromRoute] Guid id, [FromQuery] bool isActive, CancellationToken ct)
    {
        var p = await _db.PointPackages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (p is null) return NotFound(new AppError(ErrorCodes.NotFound, "Package not found."));
        p.IsActive = isActive;
        await _db.SaveChangesAsync(true, ct);
        return Ok();
    }
}
