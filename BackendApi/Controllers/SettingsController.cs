using System.ComponentModel.DataAnnotations;
using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private static readonly HashSet<string> PublicSettingKeys = new(StringComparer.Ordinal)
    {
        "AcademicYear",
        "AllowLateSubmissions",
        "MaxFileSizeMB",
        "SystemAnnouncement"
    };

    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var settings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AcademicYear"] = string.Empty,
            ["AllowLateSubmissions"] = "false",
            ["MaxFileSizeMB"] = "10",
            ["SystemAnnouncement"] = string.Empty
        };

        var configuredSettings = await _db.SystemSettings
            .AsNoTracking()
            .Where(s => PublicSettingKeys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        foreach (var setting in configuredSettings)
        {
            settings[setting.Key] = setting.Value;
        }

        return Ok(settings);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateSetting([FromBody] UpdateSettingDto dto, CancellationToken cancellationToken)
    {
        if (!PublicSettingKeys.Contains(dto.Key))
        {
            return BadRequest(new { message = "This setting key is not supported." });
        }

        var validationError = ValidateValue(dto.Key, dto.Value);
        if (validationError is not null) return BadRequest(new { message = validationError });

        var existing = await _db.SystemSettings.SingleOrDefaultAsync(s => s.Key == dto.Key, cancellationToken);
        if (existing is null)
        {
            _db.SystemSettings.Add(new SystemSetting { Key = dto.Key, Value = dto.Value.Trim() });
        }
        else
        {
            existing.Value = dto.Value.Trim();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Setting updated successfully" });
    }

    private static string? ValidateValue(string key, string value) => key switch
    {
        "AllowLateSubmissions" when !bool.TryParse(value, out _) => "AllowLateSubmissions must be true or false.",
        "MaxFileSizeMB" when !int.TryParse(value, out var size) || size is < 1 or > 100 => "MaxFileSizeMB must be between 1 and 100.",
        _ => null
    };
}

public sealed class UpdateSettingDto
{
    [Required, StringLength(100)]
    public string Key { get; init; } = string.Empty;

    [Required, StringLength(2_000)]
    public string Value { get; init; } = string.Empty;
}
