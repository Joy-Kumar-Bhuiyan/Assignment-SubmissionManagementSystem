using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
    private readonly AuditService _audit;
    private readonly INotificationService _notifications;

    public SubmissionsController(
        AppDbContext db,
        AccessControlService access,
        AuditService audit,
        INotificationService notifications)
    {
        _db = db;
        _access = access;
        _audit = audit;
        _notifications = notifications;
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
            var previousStatus = existingSubmission.Status;
            existingSubmission.Status = SubmissionStatuses.UnderReview;
            existingSubmission.IsLate = isLate;
            existingSubmission.Marks = null;
            existingSubmission.Feedback = null;

            AddStatusHistory(existingSubmission.Id, previousStatus, existingSubmission.Status);
            _audit.Add(User, "submission.resubmitted", "Submission", existingSubmission.Id, "Submission revised and returned to review.");
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(ToDto(existingSubmission));
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
        AddStatusHistory(submission.Id, null, submission.Status);
        _audit.Add(User, "submission.created", "Submission", submission.Id, "Submission created.");
        await _db.SaveChangesAsync(cancellationToken);
        await _notifications.SubmissionCreatedAsync(submission, cancellationToken);
        return Ok(ToDto(submission));
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
            .Select(s => new SubmissionResponseDto(
                s.Id, s.AssignmentId, s.StudentEmail, s.AnswerText, s.Status,
                s.Marks, s.Feedback, s.IsLate, s.SubmittedAt))
            .ToListAsync(cancellationToken);

        return Ok(submissions);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<IActionResult> GetAllSubmissions(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        [FromQuery, RegularExpression("^(UnderReview|Returned|Graded|Rejected)$")] string? status = null,
        [FromQuery, Range(1, int.MaxValue)] int? assignmentId = null,
        [FromQuery, StringLength(100)] string? studentSearch = null,
        [FromQuery] bool? isLate = null,
        [FromQuery, RegularExpression("^(submittedAt|studentEmail|status)$")] string sortBy = "submittedAt",
        [FromQuery, RegularExpression("^(asc|desc)$")] string sortDirection = "desc",
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

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
        if (assignmentId.HasValue) query = query.Where(item => item.AssignmentId == assignmentId);
        if (!string.IsNullOrWhiteSpace(studentSearch))
        {
            var term = studentSearch.Trim().ToLower();
            query = query.Where(item => item.StudentEmail.ToLower().Contains(term));
        }
        if (isLate.HasValue) query = query.Where(item => item.IsLate == isLate);

        var totalItems = await query.CountAsync(cancellationToken);
        query = (sortBy, sortDirection) switch
        {
            ("studentEmail", "asc") => query.OrderBy(item => item.StudentEmail),
            ("studentEmail", _) => query.OrderByDescending(item => item.StudentEmail),
            ("status", "asc") => query.OrderBy(item => item.Status),
            ("status", _) => query.OrderByDescending(item => item.Status),
            ("submittedAt", "asc") => query.OrderBy(item => item.SubmittedAt),
            _ => query.OrderByDescending(item => item.SubmittedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionResponseDto(
                s.Id, s.AssignmentId, s.StudentEmail, s.AnswerText, s.Status,
                s.Marks, s.Feedback, s.IsLate, s.SubmittedAt))
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

        var previousStatus = submission.Status;
        submission.Marks = grade.Marks;
        submission.Feedback = grade.Feedback.Trim();
        submission.Status = SubmissionStatuses.Graded;

        AddStatusHistory(submission.Id, previousStatus, submission.Status);
        _audit.Add(User, "submission.graded", "Submission", submission.Id, "Submission graded.");
        await _db.SaveChangesAsync(cancellationToken);
        await _notifications.SubmissionGradedAsync(submission, cancellationToken);
        return Ok(ToDto(submission));
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

        var previousStatus = submission.Status;
        submission.Status = dto.Status;
        submission.Marks = null;
        AddStatusHistory(submission.Id, previousStatus, submission.Status);
        _audit.Add(
            User,
            dto.Status == SubmissionStatuses.Returned ? "submission.returned" : "submission.rejected",
            "Submission",
            submission.Id,
            dto.Status == SubmissionStatuses.Returned ? "Submission returned for revision." : "Submission rejected.");
        await _db.SaveChangesAsync(cancellationToken);

        if (dto.Status == SubmissionStatuses.Returned)
        {
            await _notifications.SubmissionReturnedAsync(submission, cancellationToken);
        }

        return Ok(ToDto(submission));
    }

    [HttpGet("{id:int}/history")]
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<IActionResult> GetStatusHistory(int id, CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .Include(item => item.Assignment)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (submission?.Assignment is null) return NotFound(new { message = "Submission not found." });
        if (!await _access.CanManageAssignmentAsync(User, submission.Assignment, cancellationToken)) return Forbid();

        var history = await _db.SubmissionStatusHistories
            .AsNoTracking()
            .Where(item => item.SubmissionId == id)
            .OrderBy(item => item.ChangedAtUtc)
            .Select(item => new SubmissionStatusHistoryDto(
                item.Id,
                item.SubmissionId,
                item.FromStatus,
                item.ToStatus,
                item.ChangedByUserId,
                item.ChangedByRole,
                item.ChangedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    private void AddStatusHistory(int submissionId, string? fromStatus, string toStatus)
    {
        int? userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId)
            ? parsedId
            : null;

        _db.SubmissionStatusHistories.Add(new SubmissionStatusHistory
        {
            SubmissionId = submissionId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByUserId = userId,
            ChangedByRole = User.FindFirstValue(ClaimTypes.Role) ?? "Unknown",
            ChangedAtUtc = DateTime.UtcNow
        });
    }

    private static SubmissionResponseDto ToDto(Submission submission) => new(
        submission.Id,
        submission.AssignmentId,
        submission.StudentEmail,
        submission.AnswerText,
        submission.Status,
        submission.Marks,
        submission.Feedback,
        submission.IsLate,
        submission.SubmittedAt);
}

public sealed record SubmissionResponseDto(
    int Id,
    int AssignmentId,
    string StudentEmail,
    string AnswerText,
    string Status,
    int? Marks,
    string? Feedback,
    bool IsLate,
    DateTime SubmittedAt);

public sealed record SubmissionStatusHistoryDto(
    long Id,
    int SubmissionId,
    string? FromStatus,
    string ToStatus,
    int? ChangedByUserId,
    string ChangedByRole,
    DateTime ChangedAtUtc);

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
