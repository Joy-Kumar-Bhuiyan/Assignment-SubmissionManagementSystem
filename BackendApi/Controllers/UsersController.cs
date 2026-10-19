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
[Route("api/users")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordService _passwordService;

    public UsersController(AppDbContext db, PasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsNoTracking();
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.Email, u.Role, u.ClassId })
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

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        var email = InputNormalization.NormalizeEmail(dto.Email);
        if (!AppRoles.All.Contains(dto.Role))
        {
            return BadRequest(new { message = "Role must be Admin, Teacher, or Student." });
        }

        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var classValidation = await ValidateClassAsync(dto.Role, dto.ClassId, cancellationToken);
        if (classValidation is not null) return classValidation;

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordService.Hash(dto.Password),
            Role = dto.Role,
            ClassId = dto.Role == AppRoles.Student ? dto.ClassId : null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { user.Id, user.Email, user.Role, user.ClassId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();
        if (!AppRoles.All.Contains(dto.Role)) return BadRequest(new { message = "Role must be Admin, Teacher, or Student." });

        var email = InputNormalization.NormalizeEmail(dto.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id, cancellationToken))
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var classValidation = await ValidateClassAsync(dto.Role, dto.ClassId, cancellationToken);
        if (classValidation is not null) return classValidation;

        if (user.Role == AppRoles.Admin && dto.Role != AppRoles.Admin &&
            await _db.Users.CountAsync(u => u.Role == AppRoles.Admin, cancellationToken) <= 1)
        {
            return Conflict(new { message = "The last administrator cannot be demoted." });
        }

        var oldEmail = user.Email;
        if (!string.Equals(oldEmail, email, StringComparison.Ordinal))
        {
            var allocations = await _db.TeacherAllocations
                .Where(a => a.TeacherEmail == oldEmail)
                .ToListAsync(cancellationToken);
            foreach (var allocation in allocations) allocation.TeacherEmail = email;

            var submissions = await _db.Submissions
                .Where(s => s.StudentEmail == oldEmail)
                .ToListAsync(cancellationToken);
            foreach (var submission in submissions) submission.StudentEmail = email;
        }

        user.Email = email;
        user.Role = dto.Role;
        user.ClassId = dto.Role == AppRoles.Student ? dto.ClassId : null;
        user.TokenVersion++;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = _passwordService.Hash(dto.Password);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { user.Id, user.Email, user.Role, user.ClassId });
    }

    [HttpPut("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();

        user.PasswordHash = _passwordService.Hash(dto.NewPassword);
        user.TokenVersion++;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Password updated successfully. Existing sessions were revoked." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();

        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) && currentUserId == id)
        {
            return Conflict(new { message = "You cannot delete your own signed-in account." });
        }

        if (user.Role == AppRoles.Admin &&
            await _db.Users.CountAsync(u => u.Role == AppRoles.Admin, cancellationToken) <= 1)
        {
            return Conflict(new { message = "The last administrator cannot be deleted." });
        }

        if (user.Role == AppRoles.Teacher)
        {
            var allocations = await _db.TeacherAllocations
                .Where(a => a.TeacherEmail == user.Email)
                .ToListAsync(cancellationToken);
            _db.TeacherAllocations.RemoveRange(allocations);
        }
        else if (user.Role == AppRoles.Student)
        {
            var submissions = await _db.Submissions
                .Where(s => s.StudentEmail == user.Email)
                .ToListAsync(cancellationToken);
            _db.Submissions.RemoveRange(submissions);
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "User deleted successfully." });
    }

    private async Task<IActionResult?> ValidateClassAsync(string role, int? classId, CancellationToken cancellationToken)
    {
        if (role == AppRoles.Student && classId is null)
        {
            return BadRequest(new { message = "A student must be assigned to a class." });
        }

        if (classId is not null && !await _db.Classes.AnyAsync(c => c.Id == classId, cancellationToken))
        {
            return BadRequest(new { message = "The selected class does not exist." });
        }

        return null;
    }
}

public sealed class CreateUserDto
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string Role { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? ClassId { get; init; }
}

public sealed class UpdateUserDto
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [StringLength(128, MinimumLength = 12)]
    public string? Password { get; init; }

    [Required]
    public string Role { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? ClassId { get; init; }
}

public sealed class ResetPasswordDto
{
    [Required, StringLength(128, MinimumLength = 12)]
    public string NewPassword { get; init; } = string.Empty;
}
