using System.ComponentModel.DataAnnotations;
using BackendApi.Data;
using BackendApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/audit-events")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class AuditEventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditEventsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 25,
        [FromQuery, StringLength(80)] string? action = null,
        [FromQuery, StringLength(80)] string? entityType = null,
        [FromQuery, Range(1, int.MaxValue)] int? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
        {
            return BadRequest(new { message = "fromDate must be earlier than or equal to toDate." });
        }

        var query = _db.AuditEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalizedAction = action.Trim().ToLowerInvariant();
            query = query.Where(item => item.Action.ToLower() == normalizedAction);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var normalizedEntityType = entityType.Trim().ToLowerInvariant();
            query = query.Where(item => item.EntityType.ToLower() == normalizedEntityType);
        }

        if (userId.HasValue) query = query.Where(item => item.PerformedByUserId == userId);
        if (fromDate.HasValue) query = query.Where(item => item.CreatedAtUtc >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue) query = query.Where(item => item.CreatedAtUtc <= toDate.Value.ToUniversalTime());

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.Id,
                item.Action,
                item.EntityType,
                item.EntityId,
                item.PerformedByUserId,
                item.PerformedByRole,
                item.Description,
                item.CreatedAtUtc
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
}
