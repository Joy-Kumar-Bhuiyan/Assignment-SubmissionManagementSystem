using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Security;
using BackendApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace BackendApi.Tests;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public static string CurrentRole { get; set; } = AppRoles.Student;
    public static string CurrentEmail { get; set; } = "student@school.com";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, CurrentEmail),
            new Claim(ClaimTypes.Email, CurrentEmail),
            new Claim(ClaimTypes.Role, CurrentRole)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
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

    [Fact]
    public async Task Student_Cannot_Delete_Assignment()
    {
        SetIdentity(AppRoles.Student, "student-delete@school.com");
        var response = await _client.DeleteAsync("/api/assignments/1");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_Cannot_Access_Admin_User_Management()
    {
        SetIdentity(AppRoles.Teacher, "teacher-admin@school.com");
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_Cannot_Read_Teacher_Allocations()
    {
        SetIdentity(AppRoles.Student, "student-allocation@school.com");
        var response = await _client.GetAsync("/api/allocations");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_Cannot_Create_Assignment_Outside_Allocation()
    {
        await SeedAcademicContextAsync(1100, "teacher-outside@school.com", "student-outside@school.com", addAllocation: false);
        SetIdentity(AppRoles.Teacher, "teacher-outside@school.com");

        var response = await _client.PostAsJsonAsync("/api/assignments", new
        {
            Title = "Unauthorized Assignment",
            Description = "Should be blocked",
            MaxMarks = 10,
            DueDate = DateTime.UtcNow.AddDays(2),
            IsDraft = false,
            ClassId = 1100,
            SubjectId = 1100
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_Cannot_Submit_To_Another_Class()
    {
        await SeedAcademicContextAsync(1200, "teacher-class@school.com", "student-class@school.com", addAllocation: true);
        await SeedAsync(db =>
        {
            db.Classes.Add(new ClassEntity { Id = 1201, Name = "Other", Section = "B" });
            db.Assignments.Add(new Assignment
            {
                Id = 1200,
                Title = "Other class assignment",
                MaxMarks = 20,
                ClassId = 1201,
                SubjectId = 1200,
                DueDate = DateTime.UtcNow.AddDays(1)
            });
            return db.SaveChangesAsync();
        });

        SetIdentity(AppRoles.Student, "student-class@school.com");
        var response = await _client.PostAsJsonAsync("/api/submissions", new
        {
            AssignmentId = 1200,
            AnswerText = "I should not be able to submit this."
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cannot_Grade_Beyond_Max_Marks()
    {
        await SeedAcademicContextAsync(1300, "grading-teacher@school.com", "grading-student@school.com", addAllocation: true);
        await SeedAssignmentAndSubmissionAsync(1300, 1300, "grading-student@school.com", maxMarks: 50);
        SetIdentity(AppRoles.Teacher, "grading-teacher@school.com");

        var response = await _client.PutAsJsonAsync("/api/submissions/1300/grade", new
        {
            Marks = 75,
            Feedback = "Exceeds max marks!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Negative_Marks_Are_Rejected_By_Validation()
    {
        await SeedAcademicContextAsync(1400, "negative-teacher@school.com", "negative-student@school.com", addAllocation: true);
        await SeedAssignmentAndSubmissionAsync(1400, 1400, "negative-student@school.com", maxMarks: 50);
        SetIdentity(AppRoles.Teacher, "negative-teacher@school.com");

        var response = await _client.PutAsJsonAsync("/api/submissions/1400/grade", new
        {
            Marks = -1,
            Feedback = "Invalid"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Teacher_Cannot_Grade_Outside_Allocation()
    {
        await SeedAcademicContextAsync(1500, "unallocated-teacher@school.com", "allocated-student@school.com", addAllocation: false);
        await SeedAssignmentAndSubmissionAsync(1500, 1500, "allocated-student@school.com", maxMarks: 50);
        SetIdentity(AppRoles.Teacher, "unallocated-teacher@school.com");

        var response = await _client.PutAsJsonAsync("/api/submissions/1500/grade", new
        {
            Marks = 45,
            Feedback = "Should be blocked"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Arbitrary_Submission_Status_Is_Rejected()
    {
        await SeedAcademicContextAsync(1600, "status-teacher@school.com", "status-student@school.com", addAllocation: true);
        await SeedAssignmentAndSubmissionAsync(1600, 1600, "status-student@school.com", maxMarks: 50);
        SetIdentity(AppRoles.Teacher, "status-teacher@school.com");

        var response = await _client.PutAsJsonAsync("/api/submissions/1600/status", new { Status = "ApprovedBypass" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Student_Cannot_Resubmit_Graded_Work()
    {
        await SeedAcademicContextAsync(1700, "graded-teacher@school.com", "graded-student@school.com", addAllocation: true);
        await SeedAssignmentAndSubmissionAsync(
            1700,
            1700,
            "graded-student@school.com",
            maxMarks: 50,
            status: SubmissionStatuses.Graded);
        SetIdentity(AppRoles.Student, "graded-student@school.com");

        var response = await _client.PostAsJsonAsync("/api/submissions", new
        {
            AssignmentId = 1700,
            AnswerText = "Attempted replacement"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Oversized_Page_Request_Is_Rejected()
    {
        SetIdentity(AppRoles.Admin, "admin-pages@school.com");
        var response = await _client.GetAsync("/api/users?pageSize=100000");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Is_Rate_Limited_After_Five_Attempts()
    {
        HttpResponseMessage? response = null;
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                Email = "missing-user@school.com",
                Password = "WrongPassword123!"
            });
        }

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Cannot_Submit_After_Deadline_When_Late_Submissions_Disabled()
    {
        await SeedAcademicContextAsync(1800, "late-teacher@school.com", "late-student@school.com", addAllocation: true);
        await SeedAsync(db =>
        {
            db.Assignments.Add(new Assignment
            {
                Id = 1800,
                Title = "Expired History Essay",
                MaxMarks = 100,
                ClassId = 1800,
                SubjectId = 1800,
                DueDate = DateTime.UtcNow.AddDays(-2)
            });
            db.SystemSettings.Add(new SystemSetting { Id = 1800, Key = "AllowLateSubmissions", Value = "false" });
            return db.SaveChangesAsync();
        });
        SetIdentity(AppRoles.Student, "late-student@school.com");

        var response = await _client.PostAsJsonAsync("/api/submissions", new
        {
            AssignmentId = 1800,
            AnswerText = "My late answer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Complete_Submission_Workflow_Create_Update_And_Grade()
    {
        await SeedAcademicContextAsync(1900, "workflow-teacher@school.com", "workflow-student@school.com", addAllocation: true);
        await SeedAsync(db =>
        {
            db.Assignments.Add(new Assignment
            {
                Id = 1900,
                Title = "Physics Lab Report",
                MaxMarks = 100,
                ClassId = 1900,
                SubjectId = 1900,
                DueDate = DateTime.UtcNow.AddDays(5)
            });
            return db.SaveChangesAsync();
        });

        SetIdentity(AppRoles.Student, "workflow-student@school.com");
        var createResponse = await _client.PostAsJsonAsync("/api/submissions", new
        {
            AssignmentId = 1900,
            AnswerText = "Draft 1 Answer"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResponse = await _client.PostAsJsonAsync("/api/submissions", new
        {
            AssignmentId = 1900,
            AnswerText = "Final Revised Answer"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submissionId = await ReadAsync(db => Task.FromResult(db.Submissions
            .Single(s => s.AssignmentId == 1900 && s.StudentEmail == "workflow-student@school.com").Id));

        SetIdentity(AppRoles.Teacher, "workflow-teacher@school.com");
        var gradeResponse = await _client.PutAsJsonAsync($"/api/submissions/{submissionId}/grade", new
        {
            Marks = 95,
            Feedback = "Excellent work!"
        });
        gradeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void SetIdentity(string role, string email)
    {
        TestAuthHandler.CurrentRole = role;
        TestAuthHandler.CurrentEmail = email;
    }

    private Task SeedAcademicContextAsync(int id, string teacherEmail, string studentEmail, bool addAllocation) => SeedAsync(db =>
    {
        db.Classes.Add(new ClassEntity { Id = id, Name = $"Class {id}", Section = "A" });
        db.Subjects.Add(new Subject { Id = id, Name = $"Subject {id}", Code = $"S{id}" });
        db.Users.AddRange(
            new User { Id = id * 2, Email = teacherEmail, Role = AppRoles.Teacher, PasswordHash = "unused" },
            new User { Id = id * 2 + 1, Email = studentEmail, Role = AppRoles.Student, ClassId = id, PasswordHash = "unused" });
        if (addAllocation)
        {
            db.TeacherAllocations.Add(new TeacherAllocation
            {
                Id = id,
                TeacherEmail = teacherEmail,
                ClassId = id,
                SubjectId = id
            });
        }

        return db.SaveChangesAsync();
    });

    private Task SeedAssignmentAndSubmissionAsync(
        int assignmentId,
        int academicId,
        string studentEmail,
        int maxMarks,
        string status = SubmissionStatuses.UnderReview) => SeedAsync(db =>
    {
        db.Assignments.Add(new Assignment
        {
            Id = assignmentId,
            Title = $"Assignment {assignmentId}",
            MaxMarks = maxMarks,
            ClassId = academicId,
            SubjectId = academicId,
            DueDate = DateTime.UtcNow.AddDays(1)
        });
        db.Submissions.Add(new Submission
        {
            Id = assignmentId,
            AssignmentId = assignmentId,
            StudentEmail = studentEmail,
            AnswerText = "Answer",
            Status = status
        });
        return db.SaveChangesAsync();
    });

    private async Task SeedAsync(Func<AppDbContext, Task> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    private async Task<T> ReadAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }
}
