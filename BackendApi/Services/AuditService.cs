using System.Security.Claims;
using BackendApi.Data;
using BackendApi.Models;

namespace BackendApi.Services;

public sealed class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public void Add(
        ClaimsPrincipal principal,
        string action,
        string entityType,
        object entityId,
        string description)
    {
        int? userId = int.TryParse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier),
            out var parsedUserId)
            ? parsedUserId
            : null;

        _db.AuditEvents.Add(new AuditEvent
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString() ?? string.Empty,
            PerformedByUserId = userId,
            PerformedByRole = principal.FindFirstValue(ClaimTypes.Role) ?? "Unknown",
            Description = description,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
