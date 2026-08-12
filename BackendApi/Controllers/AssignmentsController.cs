using BackendApi.Data;
using BackendApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AssignmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAssignments()
    {
        var list = await _db.Assignments
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Description,
                a.MaxMarks,
                a.CreatedDate,
                a.ClassId,
                ClassName = a.Class != null ? $"{a.Class.Name} ({a.Class.Section})" : "General",
                a.SubjectId,
                SubjectName = a.Subject != null ? a.Subject.Name : "General"
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentDto dto)
    {
        var assignment = new Assignment
        {
            Title = dto.Title,
            Description = dto.Description,
            MaxMarks = dto.MaxMarks,
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        return Ok(assignment);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment == null) return NotFound();

        var relatedSubmissions = _db.Submissions.Where(s => s.AssignmentId == id);
        _db.Submissions.RemoveRange(relatedSubmissions);

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Assignment deleted" });
    }
}

public record CreateAssignmentDto(
    string Title, 
    string Description, 
    int MaxMarks, 
    int? ClassId, 
    int? SubjectId
);