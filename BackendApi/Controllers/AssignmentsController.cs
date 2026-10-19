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
[Route("api/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AccessControlService _access;

    public AssignmentsController(AppDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAssignments(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var email = User.GetEmail();
        var query = _db.Assignments
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .AsQueryable();

        if (User.IsInRole(AppRoles.Student))
        {
            var studentClassId = await _db.Users
                .Where(u => u.Email == email && u.Role == AppRoles.Student)
                .Select(u => u.ClassId)
                .SingleOrDefaultAsync(cancellationToken);

            query = query.Where(a => !a.IsDraft && (a.ClassId == null || a.ClassId == studentClassId));
        }
        else if (User.IsInRole(AppRoles.Teacher))
        {
            query = query.Where(a => a.ClassId != null && a.SubjectId != null &&
                _db.TeacherAllocations.Any(allocation =>
                    allocation.TeacherEmail == email &&
                    allocation.ClassId == a.ClassId &&
                    allocation.SubjectId == a.SubjectId));
        }
        else if (!User.IsInRole(AppRoles.Admin))
        {
            return Forbid();
        }

        var totalItems = await query.CountAsync(cancellationToken);
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAssignmentById(int id, CancellationToken cancellationToken)
    {
        var assignment = await _db.Assignments
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (assignment is null) return NotFound(new { message = "Assignment not found." });

        if (User.IsInRole(AppRoles.Student))
        {
            var email = User.GetEmail();
            if (string.IsNullOrWhiteSpace(email) ||
                !await _access.CanStudentAccessAssignmentAsync(email, assignment, cancellationToken))
            {
                return Forbid();
            }
        }
        else if (!await _access.CanManageAssignmentAsync(User, assignment, cancellationToken))
        {
            return Forbid();
        }

        return Ok(new
        {
            assignment.Id,
            assignment.Title,
            assignment.Description,
            assignment.MaxMarks,
            assignment.CreatedDate,
            assignment.DueDate,
            assignment.IsDraft,
            assignment.ClassId,
            ClassName = assignment.Class != null ? $"{assignment.Class.Name} ({assignment.Class.Section})" : "General",
            assignment.SubjectId,
            SubjectName = assignment.Subject?.Name ?? "General"
        });
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<IActionResult> CreateAssignment([FromBody] UpsertAssignmentDto dto, CancellationToken cancellationToken)
    {
        var targetError = await ValidateTargetAsync(dto.ClassId, dto.SubjectId, cancellationToken);
        if (targetError is not null) return targetError;

        var assignment = new Assignment
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            MaxMarks = dto.MaxMarks,
            DueDate = dto.DueDate?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(7),
            CreatedDate = DateTime.UtcNow,
            IsDraft = dto.IsDraft,
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId
        };

        if (!await _access.CanManageAssignmentAsync(User, assignment, cancellationToken)) return Forbid();

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(assignment);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpsertAssignmentDto dto, CancellationToken cancellationToken)
    {
        var assignment = await _db.Assignments.FindAsync([id], cancellationToken);
        if (assignment is null) return NotFound();
        if (!await _access.CanManageAssignmentAsync(User, assignment, cancellationToken)) return Forbid();

        var targetError = await ValidateTargetAsync(dto.ClassId, dto.SubjectId, cancellationToken);
        if (targetError is not null) return targetError;

        var proposedTarget = new Assignment { ClassId = dto.ClassId, SubjectId = dto.SubjectId };
        if (!await _access.CanManageAssignmentAsync(User, proposedTarget, cancellationToken)) return Forbid();

        assignment.Title = dto.Title.Trim();
        assignment.Description = dto.Description.Trim();
        assignment.MaxMarks = dto.MaxMarks;
        assignment.DueDate = dto.DueDate?.ToUniversalTime() ?? assignment.DueDate;
        assignment.IsDraft = dto.IsDraft;
        assignment.ClassId = dto.ClassId;
        assignment.SubjectId = dto.SubjectId;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(assignment);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<IActionResult> DeleteAssignment(int id, CancellationToken cancellationToken)
    {
        var assignment = await _db.Assignments.FindAsync([id], cancellationToken);
        if (assignment is null) return NotFound();
        if (!await _access.CanManageAssignmentAsync(User, assignment, cancellationToken)) return Forbid();

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Assignment deleted" });
    }

    [HttpGet("{id:int}/submissions")]
    [Authorize(Roles = $"{AppRoles.Teacher},{AppRoles.Admin}")]
    public async Task<IActionResult> GetAssignmentSubmissions(int id, CancellationToken cancellationToken)
    {
        var assignment = await _db.Assignments.FindAsync([id], cancellationToken);
        if (assignment is null) return NotFound(new { message = "Assignment not found." });
        if (!await _access.CanManageAssignmentAsync(User, assignment, cancellationToken)) return Forbid();

        var submissions = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.AssignmentId == id)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(cancellationToken);

        return Ok(submissions);
    }

    private async Task<IActionResult?> ValidateTargetAsync(int? classId, int? subjectId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.Teacher) && (classId is null || subjectId is null))
        {
            return BadRequest(new { message = "Teachers must select both an allocated class and subject." });
        }

        if (classId is not null && !await _db.Classes.AnyAsync(c => c.Id == classId, cancellationToken))
        {
            return BadRequest(new { message = "The selected class does not exist." });
        }

        if (subjectId is not null && !await _db.Subjects.AnyAsync(s => s.Id == subjectId, cancellationToken))
        {
            return BadRequest(new { message = "The selected subject does not exist." });
        }

        return null;
    }
}

public sealed class UpsertAssignmentDto
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [StringLength(10_000)]
    public string Description { get; init; } = string.Empty;

    [Range(1, 10_000)]
    public int MaxMarks { get; init; }

    public DateTime? DueDate { get; init; }
    public bool IsDraft { get; init; }

    [Range(1, int.MaxValue)]
    public int? ClassId { get; init; }

    [Range(1, int.MaxValue)]
    public int? SubjectId { get; init; }
}
