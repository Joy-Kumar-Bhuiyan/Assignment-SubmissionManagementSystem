using BackendApi.Models;
using BackendApi.Services;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, PasswordService passwordService, IConfiguration configuration)
    {
        if (await db.Users.AnyAsync()) return;

        var adminPassword = GetRequiredSeedPassword(configuration, "AdminPassword");
        var teacherPassword = GetRequiredSeedPassword(configuration, "TeacherPassword");
        var studentPassword = GetRequiredSeedPassword(configuration, "StudentPassword");

        db.Users.AddRange(
            new User
            {
                Email = "admin@school.com",
                PasswordHash = passwordService.Hash(adminPassword),
                Role = "Admin"
            },
            new User
            {
                Email = "teacher@school.com",
                PasswordHash = passwordService.Hash(teacherPassword),
                Role = "Teacher"
            },
            new User
            {
                Email = "student@school.com",
                PasswordHash = passwordService.Hash(studentPassword),
                Role = "Student"
            }
        );

        await db.SaveChangesAsync();
    }

    private static string GetRequiredSeedPassword(IConfiguration configuration, string key)
    {
        var password = configuration[$"SeedData:{key}"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            throw new InvalidOperationException($"SeedData:{key} is required and must be at least 12 characters when demo seeding is enabled.");
        }

        return password;
    }
}
