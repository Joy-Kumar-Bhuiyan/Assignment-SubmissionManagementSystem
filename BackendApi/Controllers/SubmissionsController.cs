using System.Security.Claims;
using BackendApi.Data;
using BackendApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/submissions")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubmissionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitAssignment([FromBody] CreateSubmissionDto dto)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value 
                 ?? User.FindFirst(ClaimTypes.Name)?.Value 
                 ?? User.Identity?.Name;

        var assignment = await _db.Assignments.FindAsync(dto.AssignmentId);
        if (assignment == null) return NotFound(new { message = "Assignment not found." });

        if (assignment.IsDraft) return BadRequest(new { message = "Cannot submit to a draft assignment." });

        // 1. Deadline & Late Check
        bool isLate = false;
        if (assignment.DueDate.HasValue && DateTime.UtcNow > assignment.DueDate.Value)
        {
            isLate = true;
            var allowLateSetting = await _db.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "AllowLateSubmissions");
            bool allowLate = allowLateSetting == null || allowLateSetting.Value.ToLower() == "true";

            if (!allowLate)
            {
                return BadRequest(new { message = "The deadline for this assignment has passed and late submissions are disabled." });
            }
        }

        // 2. Resubmission / Update Existing Submission
        var existingSub = await _db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == dto.AssignmentId && s.StudentEmail == email);

        if (existingSub != null)
        {
            existingSub.AnswerText = dto.AnswerText;
            existingSub.SubmittedAt = DateTime.UtcNow;
            existingSub.Status = isLate ? "Resubmitted (Late)" : "Resubmitted";

            await _db.SaveChangesAsync();
            return Ok(existingSub);
        }

        // 3. First-time Submission
        var sub = new Submission
        {
            AssignmentId = dto.AssignmentId,
            AnswerText = dto.AnswerText,
            StudentEmail = !string.IsNullOrEmpty(email) ? email : "student@school.com",
            Status = isLate ? "Submitted (Late)" : "Submitted",
            SubmittedAt = DateTime.UtcNow
        };

        _db.Submissions.Add(sub);
        await _db.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySubmissions()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value 
                 ?? User.FindFirst(ClaimTypes.Name)?.Value 
                 ?? User.Identity?.Name;

        var list = await _db.Submissions
            .Where(s => s.StudentEmail == email)
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetAllSubmissions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Submissions.AsQueryable();

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

    [HttpPut("{id}/grade")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GradeSubmission(int id, [FromBody] GradeDto grade)
    {
        var sub = await _db.Submissions.FindAsync(id);
        if (sub == null) return NotFound();

        sub.Marks = grade.Marks;
        sub.Feedback = grade.Feedback;
        sub.Status = !string.IsNullOrEmpty(grade.Status) ? grade.Status : "Graded";

        await _db.SaveChangesAsync();
        return Ok(sub);
    }
}

public record CreateSubmissionDto(int AssignmentId, string AnswerText);
public record GradeDto(int? Marks, string Feedback, string? Status);