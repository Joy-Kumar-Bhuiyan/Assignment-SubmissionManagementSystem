using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Xml.Serialization;
using BackendApi.Data;
using BackendApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize]
public sealed class IntegrationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public IntegrationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("assignments/export")]
    [Produces("application/json", "application/xml")]
    public async Task<IActionResult> ExportAssignments(
        [FromQuery, Required] string format,
        CancellationToken cancellationToken)
    {
        var normalizedFormat = format.Trim().ToLowerInvariant();
        if (normalizedFormat is not ("json" or "xml"))
        {
            return BadRequest(new { message = "format must be json or xml." });
        }

        var email = User.GetEmail();
        var query = _db.Assignments.AsNoTracking().AsQueryable();

        if (User.IsInRole(AppRoles.Student))
        {
            var classId = await _db.Users
                .Where(user => user.Email == email && user.Role == AppRoles.Student)
                .Select(user => user.ClassId)
                .SingleOrDefaultAsync(cancellationToken);
            query = query.Where(item => !item.IsDraft && (item.ClassId == null || item.ClassId == classId));
        }
        else if (User.IsInRole(AppRoles.Teacher))
        {
            query = query.Where(item => item.ClassId != null && item.SubjectId != null &&
                _db.TeacherAllocations.Any(allocation =>
                    allocation.TeacherEmail == email &&
                    allocation.ClassId == item.ClassId &&
                    allocation.SubjectId == item.SubjectId));
        }
        else if (!User.IsInRole(AppRoles.Admin))
        {
            return Forbid();
        }

        var items = await query
            .OrderBy(item => item.Id)
            .Select(item => new AssignmentExportItem
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                MaxMarks = item.MaxMarks,
                CreatedDateUtc = item.CreatedDate,
                DueDateUtc = item.DueDate,
                IsDraft = item.IsDraft,
                ClassId = item.ClassId,
                ClassName = item.Class == null ? "General" : item.Class.Name + " (" + item.Class.Section + ")",
                SubjectId = item.SubjectId,
                SubjectName = item.Subject == null ? "General" : item.Subject.Name
            })
            .ToListAsync(cancellationToken);

        var document = new AssignmentExportDocument { Assignments = items };
        if (normalizedFormat == "json") return Ok(document);

        var serializer = new XmlSerializer(typeof(AssignmentExportDocument));
        using var writer = new Utf8StringWriter();
        serializer.Serialize(writer, document);
        return Content(writer.ToString(), "application/xml", Encoding.UTF8);
    }
}

[XmlRoot("assignmentExport")]
public sealed class AssignmentExportDocument
{
    [XmlElement("assignment")]
    public List<AssignmentExportItem> Assignments { get; set; } = [];
}

public sealed class AssignmentExportItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxMarks { get; set; }
    public DateTime CreatedDateUtc { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public bool IsDraft { get; set; }
    public int? ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int? SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
}

public sealed class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}
