using BackendApi.Data;
using BackendApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/allocations")]
[Authorize]
public class AllocationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AllocationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.TeacherAllocations
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .Select(a => new
            {
                a.Id,
                a.TeacherEmail,
                a.ClassId,
                ClassName = a.Class != null ? $"{a.Class.Name} ({a.Class.Section})" : "",
                a.SubjectId,
                SubjectName = a.Subject != null ? a.Subject.Name : ""
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAllocationDto dto)
    {
        var allocation = new TeacherAllocation
        {
            TeacherEmail = dto.TeacherEmail,
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId
        };

        _db.TeacherAllocations.Add(allocation);
        await _db.SaveChangesAsync();

        return Ok(allocation);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.TeacherAllocations.FindAsync(id);
        if (item == null) return NotFound();

        _db.TeacherAllocations.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Allocation deleted" });
    }
}

public record CreateAllocationDto(string TeacherEmail, int ClassId, int SubjectId);