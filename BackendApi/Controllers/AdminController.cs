using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordService _passwordService;

    public AdminController(AppDbContext db, PasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    // -------------------------------------------------------------
    // 1. USER MANAGEMENT CRUD
    // -------------------------------------------------------------

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.ClassId
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "User with this email already exists." });
        }

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = _passwordService.HashPassword(dto.Password),
            Role = dto.Role,
            ClassId = dto.ClassId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Email, user.Role, user.ClassId });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateAdminUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Email = dto.Email;
        user.Role = dto.Role;
        user.ClassId = dto.ClassId;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = _passwordService.HashPassword(dto.Password);
        }

        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.Email, user.Role, user.ClassId });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deleted successfully." });
    }

    // -------------------------------------------------------------
    // 2. CLASS MANAGEMENT CRUD
    // -------------------------------------------------------------

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
    {
        return Ok(await _db.Classes.ToListAsync());
    }

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass([FromBody] UpsertClassDto dto)
    {
        var c = new Class { Name = dto.Name, Section = dto.Section };
        _db.Classes.Add(c);
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    [HttpPut("classes/{id}")]
    public async Task<IActionResult> UpdateClass(int id, [FromBody] UpsertClassDto dto)
    {
        var c = await _db.Classes.FindAsync(id);
        if (c == null) return NotFound();

        c.Name = dto.Name;
        c.Section = dto.Section;
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    [HttpDelete("classes/{id}")]
    public async Task<IActionResult> DeleteClass(int id)
    {
        var c = await _db.Classes.FindAsync(id);
        if (c == null) return NotFound();

        _db.Classes.Remove(c);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Class deleted." });
    }

    // -------------------------------------------------------------
    // 3. SUBJECT MANAGEMENT CRUD
    // -------------------------------------------------------------

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        return Ok(await _db.Subjects.ToListAsync());
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject([FromBody] UpsertSubjectDto dto)
    {
        var s = new Subject { Name = dto.Name, Code = dto.Code };
        _db.Subjects.Add(s);
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpPut("subjects/{id}")]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpsertSubjectDto dto)
    {
        var s = await _db.Subjects.FindAsync(id);
        if (s == null) return NotFound();

        s.Name = dto.Name;
        s.Code = dto.Code;
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpDelete("subjects/{id}")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var s = await _db.Subjects.FindAsync(id);
        if (s == null) return NotFound();

        _db.Subjects.Remove(s);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Subject deleted." });
    }

    // -------------------------------------------------------------
    // 4. TEACHER MAPPINGS CRUD
    // -------------------------------------------------------------

    [HttpGet("teacher-mappings")]
    public async Task<IActionResult> GetTeacherMappings()
    {
        var mappings = await _db.TeacherMappings
            .Include(tm => tm.Class)
            .Include(tm => tm.Subject)
            .ToListAsync();

        return Ok(mappings);
    }

    [HttpPost("teacher-mappings")]
    public async Task<IActionResult> AssignTeacher([FromBody] AssignTeacherDto dto)
    {
        var mapping = new TeacherMapping
        {
            TeacherEmail = dto.TeacherEmail,
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId
        };

        _db.TeacherMappings.Add(mapping);
        await _db.SaveChangesAsync();
        return Ok(mapping);
    }

    [HttpDelete("teacher-mappings/{id}")]
    public async Task<IActionResult> RemoveTeacherMapping(int id)
    {
        var mapping = await _db.TeacherMappings.FindAsync(id);
        if (mapping == null) return NotFound();

        _db.TeacherMappings.Remove(mapping);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Teacher mapping removed." });
    }
    // -------------------------------------------------------------
// 5. APPLICATION SETTINGS MANAGEMENT
// -------------------------------------------------------------

[HttpGet("settings")]
public async Task<IActionResult> GetSettings()
{
    var settings = await _db.SystemSettings.ToListAsync();
    
    // Seed default settings if empty
    if (!settings.Any())
    {
        var defaultSettings = new List<SystemSetting>
        {
            new SystemSetting { Key = "AllowLateSubmissions", Value = "true" },
            new SystemSetting { Key = "MaxFileSizeMB", Value = "10" },
            new SystemSetting { Key = "SystemAnnouncement", Value = "Welcome to School Portal!" }
        };

        _db.SystemSettings.AddRange(defaultSettings);
        await _db.SaveChangesAsync();
        settings = defaultSettings;
    }

    return Ok(settings);
}

[HttpPut("settings")]
public async Task<IActionResult> UpdateSettings([FromBody] List<UpdateSettingDto> dtos)
{
    foreach (var dto in dtos)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == dto.Key);
        if (setting != null)
        {
            setting.Value = dto.Value;
        }
        else
        {
            _db.SystemSettings.Add(new SystemSetting { Key = dto.Key, Value = dto.Value });
        }
    }

    await _db.SaveChangesAsync();
    return Ok(await _db.SystemSettings.ToListAsync());
}


}

// -------------------------------------------------------------
// INLINE DTO DEFINITIONS (No external files needed)
// -------------------------------------------------------------
public record UpdateSettingDto(string Key, string Value);
public record CreateAdminUserDto(string Email, string Password, string Role, int? ClassId);
public record UpdateAdminUserDto(string Email, string? Password, string Role, int? ClassId);
public record UpsertClassDto(string Name, string Section);
public record UpsertSubjectDto(string Name, string Code);
public record AssignTeacherDto(string TeacherEmail, int ClassId, int SubjectId);