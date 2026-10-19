using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var email = User.GetEmail();
        var now = DateTime.UtcNow;
        var upcomingCutoff = now.AddDays(7);

        if (User.IsInRole(AppRoles.Admin))
        {
            var statuses = await CountSubmissionStatusesAsync(_db.Submissions.AsNoTracking(), cancellationToken);
            return Ok(new
            {
                role = AppRoles.Admin,
                students = await _db.Users.CountAsync(user => user.Role == AppRoles.Student, cancellationToken),
                teachers = await _db.Users.CountAsync(user => user.Role == AppRoles.Teacher, cancellationToken),
                assignments = await _db.Assignments.CountAsync(cancellationToken),
                publishedAssignments = await _db.Assignments.CountAsync(item => !item.IsDraft, cancellationToken),
                draftAssignments = await _db.Assignments.CountAsync(item => item.IsDraft, cancellationToken),
                totalSubmissions = await _db.Submissions.CountAsync(cancellationToken),
                submissionStatuses = statuses,
                averageMarksPercentage = await AverageMarksPercentageAsync(_db.Submissions.AsNoTracking(), cancellationToken),
                upcomingDeadlines = await _db.Assignments.CountAsync(item => !item.IsDraft && item.DueDate >= now && item.DueDate <= upcomingCutoff, cancellationToken)
            });
        }

        if (User.IsInRole(AppRoles.Teacher) && !string.IsNullOrWhiteSpace(email))
        {
            var assignments = TeacherAssignments(email);
            var submissions = _db.Submissions.AsNoTracking().Where(submission =>
                submission.Assignment != null &&
                submission.Assignment.ClassId != null &&
                submission.Assignment.SubjectId != null &&
                _db.TeacherAllocations.Any(allocation =>
                    allocation.TeacherEmail == email &&
                    allocation.ClassId == submission.Assignment.ClassId &&
                    allocation.SubjectId == submission.Assignment.SubjectId));
            var statuses = await CountSubmissionStatusesAsync(submissions, cancellationToken);

            return Ok(new
            {
                role = AppRoles.Teacher,
                assignments = await assignments.CountAsync(cancellationToken),
                publishedAssignments = await assignments.CountAsync(item => !item.IsDraft, cancellationToken),
                draftAssignments = await assignments.CountAsync(item => item.IsDraft, cancellationToken),
                totalSubmissions = await submissions.CountAsync(cancellationToken),
                submissionStatuses = statuses,
                averageMarksPercentage = await AverageMarksPercentageAsync(submissions, cancellationToken),
                upcomingDeadlines = await assignments.CountAsync(item => !item.IsDraft && item.DueDate >= now && item.DueDate <= upcomingCutoff, cancellationToken)
            });
        }

        if (User.IsInRole(AppRoles.Student) && !string.IsNullOrWhiteSpace(email))
        {
            var classId = await _db.Users
                .Where(user => user.Email == email && user.Role == AppRoles.Student)
                .Select(user => user.ClassId)
                .SingleOrDefaultAsync(cancellationToken);
            var availableAssignments = _db.Assignments.AsNoTracking()
                .Where(item => !item.IsDraft && (item.ClassId == null || item.ClassId == classId));
            var submissions = _db.Submissions.AsNoTracking().Where(item => item.StudentEmail == email);

            return Ok(new
            {
                role = AppRoles.Student,
                availableAssignments = await availableAssignments.CountAsync(cancellationToken),
                submittedAssignments = await submissions.CountAsync(cancellationToken),
                pendingResults = await submissions.CountAsync(item => item.Status == SubmissionStatuses.UnderReview, cancellationToken),
                returnedAssignments = await submissions.CountAsync(item => item.Status == SubmissionStatuses.Returned, cancellationToken),
                gradedAssignments = await submissions.CountAsync(item => item.Status == SubmissionStatuses.Graded, cancellationToken),
                upcomingDeadlines = await availableAssignments.CountAsync(item => item.DueDate >= now && item.DueDate <= upcomingCutoff, cancellationToken)
            });
        }

        return Forbid();
    }

    private IQueryable<Assignment> TeacherAssignments(string email) => _db.Assignments.AsNoTracking()
        .Where(assignment => assignment.ClassId != null && assignment.SubjectId != null &&
            _db.TeacherAllocations.Any(allocation =>
                allocation.TeacherEmail == email &&
                allocation.ClassId == assignment.ClassId &&
                allocation.SubjectId == assignment.SubjectId));

    private static async Task<object> CountSubmissionStatusesAsync(
        IQueryable<Submission> query,
        CancellationToken cancellationToken)
    {
        var counts = await query
            .GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        return new
        {
            underReview = counts.GetValueOrDefault(SubmissionStatuses.UnderReview),
            graded = counts.GetValueOrDefault(SubmissionStatuses.Graded),
            returned = counts.GetValueOrDefault(SubmissionStatuses.Returned),
            rejected = counts.GetValueOrDefault(SubmissionStatuses.Rejected)
        };
    }

    private static async Task<double?> AverageMarksPercentageAsync(
        IQueryable<Submission> query,
        CancellationToken cancellationToken)
    {
        var graded = query.Where(item =>
            item.Status == SubmissionStatuses.Graded &&
            item.Marks != null &&
            item.Assignment != null &&
            item.Assignment.MaxMarks > 0);

        if (!await graded.AnyAsync(cancellationToken)) return null;

        var average = await graded.AverageAsync(
            item => 100.0 * item.Marks!.Value / item.Assignment!.MaxMarks,
            cancellationToken);
        return Math.Round(average, 2);
    }
}
