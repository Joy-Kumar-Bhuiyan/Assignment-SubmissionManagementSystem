using System.Security.Claims;
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
    public async Task<IActionResult> GetAllAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        var email = User.FindFirst(ClaimTypes.Email)?.Value 
                 ?? User.FindFirst(ClaimTypes.Name)?.Value 
                 ?? User.Identity?.Name;

        var query = _db.Assignments
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .AsQueryable();

        // Students can only see published assignments assigned to their class (or General)
        if (userRole == "Student")
        {
            var student = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            int? studentClassId = student?.ClassId;

            // Hide drafts and only show assignments targeted to the student's class (or general assignments)
            query = query.Where(a => !a.IsDraft && (a.ClassId == null || a.ClassId == studentClassId));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Description,
                a.MaxMarks,
                a.CreatedDate,
                a.DueDate,
                a.IsDraft,
                a.ClassId,
                ClassName = a.Class != null ? $"{a.Class.Name} ({a.Class.Section})" : "General",
                a.SubjectId,
                SubjectName = a.Subject != null ? a.Subject.Name : "General"
            })
            .ToListAsync();

        return Ok(new
        {
            totalItems,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            data = items
        });
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> CreateAssignment([FromBody] UpsertAssignmentDto dto)
    {
        var assignment = new Assignment
        {
            Title = dto.Title,
            Description = dto.Description,
            MaxMarks = dto.MaxMarks,
            DueDate = dto.DueDate,
            IsDraft = dto.IsDraft,
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        return Ok(assignment);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpsertAssignmentDto dto)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment == null) return NotFound();

        assignment.Title = dto.Title;
        assignment.Description = dto.Description;
        assignment.MaxMarks = dto.MaxMarks;
        assignment.DueDate = dto.DueDate;
        assignment.IsDraft = dto.IsDraft;
        assignment.ClassId = dto.ClassId;
        assignment.SubjectId = dto.SubjectId;

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

    [HttpGet("{id}/submissions")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetAssignmentSubmissions(int id)
    {
        var submissions = await _db.Submissions
            .Where(s => s.AssignmentId == id)
            .ToListAsync();

        return Ok(submissions);
    }
}

public record UpsertAssignmentDto(
    string Title,
    string Description,
    int MaxMarks,
    DateTime? DueDate,
    bool IsDraft,
    int? ClassId,
    int? SubjectId
);