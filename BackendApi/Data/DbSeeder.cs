using BackendApi.Models;
using BackendApi.Services;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, PasswordService passwordService)
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync()) return;

        db.Users.AddRange(
            new User
            {
                Email = "admin@school.com",
                PasswordHash = passwordService.Hash("AdminPassword123!"),
                Role = "Admin"
            },
            new User
            {
                Email = "teacher@school.com",
                PasswordHash = passwordService.Hash("TeacherPassword123!"),
                Role = "Teacher"
            },
            new User
            {
                Email = "student@school.com",
                PasswordHash = passwordService.Hash("StudentPassword123!"),
                Role = "Student"
            }
        );

        await db.SaveChangesAsync();
    }
}