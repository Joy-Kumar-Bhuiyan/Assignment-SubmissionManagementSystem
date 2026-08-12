using BackendApi.Data;
using BackendApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _db.SystemSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
        return Ok(settings);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSetting([FromBody] SystemSetting setting)
    {
        var existing = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == setting.Key);
        if (existing == null)
        {
            _db.SystemSettings.Add(setting);
        }
        else
        {
            existing.Value = setting.Value;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Setting updated successfully" });
    }
}