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
[Route("api/submissions")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AccessControlService _access;

    public SubmissionsController(AppDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> SubmitAssignment([FromBody] CreateSubmissionDto dto, CancellationToken cancellationToken)
    {
        var email = User.GetEmail();
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

        var assignment = await _db.Assignments.FindAsync([dto.AssignmentId], cancellationToken);
        if (assignment is null) return NotFound(new { message = "Assignment not found." });

        if (!await _access.CanStudentAccessAssignmentAsync(email, assignment, cancellationToken))
        {
            return Forbid();
        }

        var isLate = assignment.DueDate.HasValue && DateTime.UtcNow > assignment.DueDate.Value;
        if (isLate)
        {
            var allowLateValue = await _db.SystemSettings
                .Where(s => s.Key == "AllowLateSubmissions")
                .Select(s => s.Value)
                .SingleOrDefaultAsync(cancellationToken);

            if (!bool.TryParse(allowLateValue, out var allowLate) || !allowLate)
            {
                return BadRequest(new { message = "The deadline has passed and late submissions are disabled." });
            }
        }

        var existingSubmission = await _db.Submissions
            .SingleOrDefaultAsync(s => s.AssignmentId == dto.AssignmentId && s.StudentEmail == email, cancellationToken);

        if (existingSubmission is not null)
        {
            if (existingSubmission.Status is SubmissionStatuses.Graded or SubmissionStatuses.Rejected)
            {
                return Conflict(new { message = "A graded or rejected submission cannot be changed." });
            }

            existingSubmission.AnswerText = dto.AnswerText.Trim();
            existingSubmission.SubmittedAt = DateTime.UtcNow;
            existingSubmission.Status = SubmissionStatuses.UnderReview;
            existingSubmission.IsLate = isLate;
            existingSubmission.Marks = null;
            existingSubmission.Feedback = null;

            await _db.SaveChangesAsync(cancellationToken);
            return Ok(existingSubmission);
        }

        var submission = new Submission
        {
            AssignmentId = dto.AssignmentId,
            AnswerText = dto.AnswerText.Trim(),
            StudentEmail = email,
            Status = SubmissionStatuses.UnderReview,
            IsLate = isLate,
            SubmittedAt = DateTime.UtcNow
        };

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(submission);
    }

    [HttpGet("my")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> GetMySubmissions(CancellationToken cancellationToken)
    {
        var email = User.GetEmail();
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

        var submissions = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.StudentEmail == email)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(cancellationToken);

        return Ok(submissions);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<IActionResult> GetAllSubmissions(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var email = User.GetEmail();
        var query = _db.Submissions.AsNoTracking().AsQueryable();

        if (User.IsInRole(AppRoles.Teacher))
        {
            query = query.Where(s => s.Assignment != null &&
                s.Assignment.ClassId != null && s.Assignment.SubjectId != null &&
                _db.TeacherAllocations.Any(allocation =>
                    allocation.TeacherEmail == email &&
                    allocation.ClassId == s.Assignment.ClassId &&
                    allocation.SubjectId == s.Assignment.SubjectId));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            totalItems,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            data = items
        });
    }

    [HttpPut("{id:int}/grade")]
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<IActionResult> GradeSubmission(int id, [FromBody] GradeDto grade, CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (submission?.Assignment is null) return NotFound();
        if (!await _access.CanManageAssignmentAsync(User, submission.Assignment, cancellationToken)) return Forbid();

        if (grade.Marks > submission.Assignment.MaxMarks)
        {
            return BadRequest(new { message = $"Marks cannot exceed the maximum allowed marks ({submission.Assignment.MaxMarks})." });
        }

        submission.Marks = grade.Marks;
        submission.Feedback = grade.Feedback.Trim();
        submission.Status = SubmissionStatuses.Graded;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(submission);
    }

    [HttpPatch("{id:int}/status")]
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<IActionResult> UpdateSubmissionStatus(int id, [FromBody] UpdateStatusDto dto, CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (submission?.Assignment is null) return NotFound(new { message = "Submission not found" });
        if (!await _access.CanManageAssignmentAsync(User, submission.Assignment, cancellationToken)) return Forbid();
        if (!SubmissionStatuses.TeacherReviewOutcomes.Contains(dto.Status))
        {
            return BadRequest(new { message = "Status must be Returned or Rejected. Use the grade endpoint to mark work as Graded." });
        }

        submission.Status = dto.Status;
        submission.Marks = null;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(submission);
    }
}

public sealed class CreateSubmissionDto
{
    [Range(1, int.MaxValue)]
    public int AssignmentId { get; init; }

    [Required, StringLength(50_000, MinimumLength = 1)]
    public string AnswerText { get; init; } = string.Empty;
}

public sealed class GradeDto
{
    [Range(0, 10_000)]
    public int Marks { get; init; }

    [StringLength(5_000)]
    public string Feedback { get; init; } = string.Empty;
}

public sealed class UpdateStatusDto
{
    [Required]
    public string Status { get; init; } = string.Empty;
}
