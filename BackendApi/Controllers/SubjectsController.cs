using System.ComponentModel.DataAnnotations;
using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Security;
using BackendApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/subjects")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public SubjectsController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) => Ok(await _db.Subjects
        .AsNoTracking()
        .OrderBy(s => s.Code)
        .ToListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] UpsertSubjectDto dto, CancellationToken cancellationToken)
    {
        var name = dto.Name.Trim();
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _db.Subjects.AnyAsync(s => s.Code == code, cancellationToken))
        {
            return Conflict(new { message = "This subject code already exists." });
        }

        var entity = new Subject { Name = name, Code = code };
        _db.Subjects.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        _audit.Add(User, "subject.created", "Subject", entity.Id, "Subject created.");
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertSubjectDto dto, CancellationToken cancellationToken)
    {
        var entity = await _db.Subjects.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _db.Subjects.AnyAsync(s => s.Id != id && s.Code == code, cancellationToken))
        {
            return Conflict(new { message = "This subject code already exists." });
        }

        entity.Name = dto.Name.Trim();
        entity.Code = code;
        _audit.Add(User, "subject.updated", "Subject", entity.Id, "Subject updated.");
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.Subjects.FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();

        var inUse = await _db.Assignments.AnyAsync(a => a.SubjectId == id, cancellationToken) ||
                    await _db.TeacherAllocations.AnyAsync(a => a.SubjectId == id, cancellationToken);
        if (inUse) return Conflict(new { message = "This subject is in use and cannot be deleted." });

        _audit.Add(User, "subject.deleted", "Subject", entity.Id, "Subject deleted.");
        _db.Subjects.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Subject deleted" });
    }
}

public sealed class UpsertSubjectDto
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, RegularExpression("^[A-Za-z0-9_-]{2,30}$")]
    public string Code { get; init; } = string.Empty;
}
