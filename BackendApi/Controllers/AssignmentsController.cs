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
    public async Task<IActionResult> GetAssignments()
    {
        return Ok(await _db.Assignments.ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> CreateAssignment([FromBody] Assignment assignment)
    {
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();
        return Ok(assignment);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment == null)
        {
            return NotFound();
        }

        // Delete any submissions linked to this assignment first
        var relatedSubmissions = _db.Submissions.Where(s => s.AssignmentId == id);
        _db.Submissions.RemoveRange(relatedSubmissions);

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Assignment deleted successfully" });
    }
}