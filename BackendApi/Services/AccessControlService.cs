using System.Security.Claims;
using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Security;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Services;

public sealed class AccessControlService
{
    private readonly AppDbContext _db;

    public AccessControlService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanManageAssignmentAsync(
        ClaimsPrincipal principal,
        Assignment assignment,
        CancellationToken cancellationToken = default)
    {
        if (principal.IsInRole(AppRoles.Admin)) return true;
        if (!principal.IsInRole(AppRoles.Teacher)) return false;

        var email = principal.GetEmail();
        if (string.IsNullOrWhiteSpace(email) || assignment.ClassId is null || assignment.SubjectId is null)
        {
            return false;
        }

        return await _db.TeacherAllocations.AnyAsync(a =>
            a.TeacherEmail == email &&
            a.ClassId == assignment.ClassId &&
            a.SubjectId == assignment.SubjectId,
            cancellationToken);
    }

    public async Task<bool> CanStudentAccessAssignmentAsync(
        string email,
        Assignment assignment,
        CancellationToken cancellationToken = default)
    {
        if (assignment.IsDraft) return false;

        var classId = await _db.Users
            .Where(u => u.Email == email && u.Role == AppRoles.Student)
            .Select(u => u.ClassId)
            .SingleOrDefaultAsync(cancellationToken);

        return assignment.ClassId is null || (classId is not null && assignment.ClassId == classId);
    }
}
