using BackendApi.Data;
using BackendApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BackendApi.Tests;

public sealed class DemoSeedTests
{
    [Fact]
    public async Task Seeder_CreatesConfiguredDemoAccountsWithUsablePasswords()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"seed-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedData:AdminPassword"] = "AdminDemo!2026",
                ["SeedData:TeacherPassword"] = "TeacherDemo!2026",
                ["SeedData:StudentPassword"] = "StudentDemo!2026"
            })
            .Build();
        var passwords = new PasswordService();

        await DbSeeder.SeedAsync(db, passwords, configuration);

        passwords.Verify("AdminDemo!2026", (await db.Users.SingleAsync(u => u.Email == "admin@school.com")).PasswordHash)
            .Should().BeTrue();
        passwords.Verify("TeacherDemo!2026", (await db.Users.SingleAsync(u => u.Email == "teacher@school.com")).PasswordHash)
            .Should().BeTrue();
        passwords.Verify("StudentDemo!2026", (await db.Users.SingleAsync(u => u.Email == "student@school.com")).PasswordHash)
            .Should().BeTrue();
    }
}
