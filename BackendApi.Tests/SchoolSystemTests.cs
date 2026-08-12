using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using BackendApi.Data;
using BackendApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using FluentAssertions;

namespace BackendApi.Tests;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public static string CurrentRole = "Student";
    public static string CurrentEmail = "student@school.com";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(ClaimTypes.Name, CurrentEmail),
            new Claim(ClaimTypes.Email, CurrentEmail),
            new Claim(ClaimTypes.Role, CurrentRole)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class SchoolSystemTests : IClassFixture<CustomWebApplicationFactory<BackendApi.Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<BackendApi.Program> _factory;

    public SchoolSystemTests(CustomWebApplicationFactory<BackendApi.Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------
    // 1. AUTHORIZATION TESTS
    // -------------------------------------------------------------

    [Fact]
    public async Task Student_Cannot_Delete_Assignment_Returns_Forbidden()
    {
        TestAuthHandler.CurrentRole = "Student";
        TestAuthHandler.CurrentEmail = "student@school.com";

        var response = await _client.DeleteAsync("/api/assignments/1");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_Cannot_Access_Admin_User_Management_Returns_Forbidden()
    {
        TestAuthHandler.CurrentRole = "Teacher";
        TestAuthHandler.CurrentEmail = "teacher@school.com";

        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // -------------------------------------------------------------
    // 2. BUSINESS RULE TESTS
    // -------------------------------------------------------------

    [Fact]
    public async Task Cannot_Grade_Beyond_Max_Marks_Returns_BadRequest()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            
            if (!db.Assignments.Any(a => a.Id == 99))
            {
                db.Assignments.Add(new Assignment { Id = 99, Title = "Test Math", MaxMarks = 50 });
                db.Submissions.Add(new Submission { Id = 99, AssignmentId = 99, StudentEmail = "student@school.com", AnswerText = "2+2=4" });
                await db.SaveChangesAsync();
            }
        }

        TestAuthHandler.CurrentRole = "Teacher";
        TestAuthHandler.CurrentEmail = "teacher@school.com";

        var gradePayload = new { Marks = 75, Feedback = "Exceeds max marks!", Status = "Graded" }; // MaxMarks is 50

        var response = await _client.PutAsJsonAsync("/api/submissions/99/grade", gradePayload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cannot_Submit_After_Deadline_When_Late_Submissions_Disabled()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            if (!db.Assignments.Any(a => a.Id == 101))
            {
                db.Assignments.Add(new Assignment 
                { 
                    Id = 101, 
                    Title = "Expired History Essay", 
                    MaxMarks = 100, 
                    DueDate = DateTime.UtcNow.AddDays(-2), // 2 days ago
                    IsDraft = false 
                });

                if (!db.SystemSettings.Any(s => s.Key == "AllowLateSubmissions"))
                {
                    db.SystemSettings.Add(new SystemSetting { Key = "AllowLateSubmissions", Value = "false" });
                }

                await db.SaveChangesAsync();
            }
        }

        TestAuthHandler.CurrentRole = "Student";
        TestAuthHandler.CurrentEmail = "late_student@school.com";

        var payload = new { AssignmentId = 101, AnswerText = "My late answer" };

        var response = await _client.PostAsJsonAsync("/api/submissions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------
    // 3. SUBMISSION WORKFLOW TESTS (Create, Update, Grade)
    // -------------------------------------------------------------

    [Fact]
    public async Task Complete_Submission_Workflow_Create_Update_And_Grade()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            if (!db.Assignments.Any(a => a.Id == 200))
            {
                db.Assignments.Add(new Assignment { Id = 200, Title = "Physics Lab Report", MaxMarks = 100, DueDate = DateTime.UtcNow.AddDays(5) });
                await db.SaveChangesAsync();
            }
        }

        // Step 1: Student creates initial submission
        TestAuthHandler.CurrentRole = "Student";
        TestAuthHandler.CurrentEmail = "workflow_student@school.com";

        var initialPayload = new { AssignmentId = 200, AnswerText = "Draft 1 Answer" };
        var createResponse = await _client.PostAsJsonAsync("/api/submissions", initialPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Student updates/resubmits answer before deadline
        var updatePayload = new { AssignmentId = 200, AnswerText = "Final Revised Answer" };
        var updateResponse = await _client.PostAsJsonAsync("/api/submissions", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Fetch submission ID created in DB
        int submissionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sub = db.Submissions.First(s => s.AssignmentId == 200 && s.StudentEmail == "workflow_student@school.com");
            submissionId = sub.Id;
            sub.AnswerText.Should().Be("Final Revised Answer");
        }

        // Step 3: Teacher grades the submission with valid marks
        TestAuthHandler.CurrentRole = "Teacher";
        TestAuthHandler.CurrentEmail = "grading_teacher@school.com";

        var gradePayload = new { Marks = 95, Feedback = "Excellent work!", Status = "Graded" };
        var gradeResponse = await _client.PutAsJsonAsync($"/api/submissions/{submissionId}/grade", gradePayload);
        gradeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}