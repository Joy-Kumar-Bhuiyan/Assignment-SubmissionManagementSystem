using System.ComponentModel.DataAnnotations;
using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/allocations")]
[Authorize(Roles = AppRoles.Admin)]
public class AllocationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AllocationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var allocations = await _db.TeacherAllocations
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .OrderBy(a => a.TeacherEmail)
            .Select(a => new
            {
                a.Id,
                a.TeacherEmail,
                a.ClassId,
                ClassName = a.Class != null ? $"{a.Class.Name} ({a.Class.Section})" : string.Empty,
                a.SubjectId,
                SubjectName = a.Subject != null ? a.Subject.Name : string.Empty
            })
            .ToListAsync(cancellationToken);

        return Ok(allocations);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAllocationDto dto, CancellationToken cancellationToken)
    {
        var teacherEmail = InputNormalization.NormalizeEmail(dto.TeacherEmail);
        if (!await _db.Users.AnyAsync(u => u.Email == teacherEmail && u.Role == AppRoles.Teacher, cancellationToken))
        {
            return BadRequest(new { message = "The selected teacher account does not exist." });
        }

        if (!await _db.Classes.AnyAsync(c => c.Id == dto.ClassId, cancellationToken) ||
            !await _db.Subjects.AnyAsync(s => s.Id == dto.SubjectId, cancellationToken))
        {
            return BadRequest(new { message = "The selected class or subject does not exist." });
        }

        if (await _db.TeacherAllocations.AnyAsync(a =>
            a.TeacherEmail == teacherEmail && a.ClassId == dto.ClassId && a.SubjectId == dto.SubjectId,
            cancellationToken))
        {
            return Conflict(new { message = "This teacher allocation already exists." });
        }

        var allocation = new TeacherAllocation
        {
            TeacherEmail = teacherEmail,
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId
        };

        _db.TeacherAllocations.Add(allocation);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(allocation);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var allocation = await _db.TeacherAllocations.FindAsync([id], cancellationToken);
        if (allocation is null) return NotFound();

        _db.TeacherAllocations.Remove(allocation);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Allocation deleted" });
    }
}

public sealed class CreateAllocationDto
{
    [Required, EmailAddress, StringLength(254)]
    public string TeacherEmail { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ClassId { get; init; }

    [Range(1, int.MaxValue)]
    public int SubjectId { get; init; }
}
