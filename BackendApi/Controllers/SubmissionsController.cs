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

        var sub = new Submission
        {
            AssignmentId = dto.AssignmentId,
            AnswerText = dto.AnswerText,
            StudentEmail = !string.IsNullOrEmpty(email) ? email : "student@school.com",
            Status = "Submitted"
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
    public async Task<IActionResult> GetAllSubmissions()
    {
        return Ok(await _db.Submissions.ToListAsync());
    }

    [HttpPut("{id}/grade")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GradeSubmission(int id, [FromBody] GradeDto grade)
    {
        var sub = await _db.Submissions.FindAsync(id);
        if (sub == null) return NotFound();

        sub.Marks = grade.Marks;
        sub.Feedback = grade.Feedback;
        sub.Status = "Graded";

        await _db.SaveChangesAsync();
        return Ok(sub);
    }
}

public record CreateSubmissionDto(int AssignmentId, string AnswerText);
public record GradeDto(int Marks, string Feedback);