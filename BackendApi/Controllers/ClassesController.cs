using System.ComponentModel.DataAnnotations;
using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/classes")]
[Authorize]
public class ClassesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClassesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) => Ok(await _db.Classes
        .AsNoTracking()
        .OrderBy(c => c.Name)
        .ThenBy(c => c.Section)
        .ToListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] UpsertClassDto dto, CancellationToken cancellationToken)
    {
        var name = dto.Name.Trim();
        var section = dto.Section.Trim();
        if (await _db.Classes.AnyAsync(c => c.Name == name && c.Section == section, cancellationToken))
        {
            return Conflict(new { message = "This class and section already exist." });
        }

        var entity = new ClassEntity { Name = name, Section = section };
        _db.Classes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertClassDto dto, CancellationToken cancellationToken)
    {
        var entity = await _db.Classes.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();

        var name = dto.Name.Trim();
        var section = dto.Section.Trim();
        if (await _db.Classes.AnyAsync(c => c.Id != id && c.Name == name && c.Section == section, cancellationToken))
        {
            return Conflict(new { message = "This class and section already exist." });
        }

        entity.Name = name;
        entity.Section = section;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.Classes.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();

        var inUse = await _db.Users.AnyAsync(u => u.ClassId == id, cancellationToken) ||
                    await _db.Assignments.AnyAsync(a => a.ClassId == id, cancellationToken) ||
                    await _db.TeacherAllocations.AnyAsync(a => a.ClassId == id, cancellationToken);
        if (inUse) return Conflict(new { message = "This class is in use and cannot be deleted." });

        _db.Classes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Class deleted" });
    }
}

public sealed class UpsertClassDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    public string Section { get; init; } = string.Empty;
}
