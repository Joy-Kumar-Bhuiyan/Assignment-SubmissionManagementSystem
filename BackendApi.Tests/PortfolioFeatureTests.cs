using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Security;
using BackendApi.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BackendApi.Tests;

public sealed class PortfolioFeatureTests : IClassFixture<CustomWebApplicationFactory<BackendApi.Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<BackendApi.Program> _factory;
    private readonly RecordingEmailService _emails;
    private readonly RecordingWebhookService _webhooks;

    public PortfolioFeatureTests(CustomWebApplicationFactory<BackendApi.Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _emails = factory.Services.GetRequiredService<RecordingEmailService>();
        _webhooks = factory.Services.GetRequiredService<RecordingWebhookService>();
        _emails.Reset();
        _webhooks.Reset();
    }

    [Fact]
    public async Task Student_CannotReadAuditEvents()
    {
        SetIdentity(AppRoles.Student, "audit-student@school.com");

        var response = await _client.GetAsync("/api/audit-events");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PublishingAssignment_CreatesAuditEvent_AndTriggersIntegrations()
    {
        const int id = 3100;
        const string teacher = "publish-teacher@school.com";
        const string student = "publish-student@school.com";
        await SeedAcademicContextAsync(id, teacher, student);
        SetIdentity(AppRoles.Teacher, teacher);

        var createResponse = await _client.PostAsJsonAsync("/api/assignments", new
        {
            Title = "Published integration assignment",
            Description = "A focused test",
            MaxMarks = 40,
            DueDate = DateTime.UtcNow.AddDays(2),
            IsDraft = false,
            ClassId = id,
            SubjectId = id
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _emails.Messages.Should().ContainSingle(message => message.To == student);
        _webhooks.Messages.Should().ContainSingle(message => message.Event == "assignment.published");

        SetIdentity(AppRoles.Admin, "audit-admin@school.com");
        var auditResponse = await _client.GetAsync("/api/audit-events?action=assignment.published");
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auditJson = await auditResponse.Content.ReadAsStringAsync();
        auditJson.Should().Contain("assignment.published");
    }

    [Fact]
    public async Task AuditEvent_DoesNotContainCreatedUsersPassword()
    {
        SetIdentity(AppRoles.Admin, "create-admin@school.com");
        const string password = "NeverStoreThisPassword!123";

        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            Email = "audited-user@school.com",
            Password = password,
            Role = AppRoles.Teacher
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auditResponse = await _client.GetAsync("/api/audit-events?action=user.created");
        var content = await auditResponse.Content.ReadAsStringAsync();
        content.Should().NotContain(password);
        content.ToLowerInvariant().Should().NotContain("passwordhash");
    }

    [Fact]
    public async Task DashboardSummary_UsesRoleScopedDatabaseCounts()
    {
        const int id = 3200;
        const string student = "dashboard-student@school.com";
        await SeedAcademicContextAsync(id, "dashboard-teacher@school.com", student);
        await SeedAsync(db =>
        {
            db.Assignments.AddRange(
                new Assignment { Id = id, Title = "Visible", MaxMarks = 10, ClassId = id, SubjectId = id, DueDate = DateTime.UtcNow.AddDays(2) },
                new Assignment { Id = id + 1, Title = "Draft", MaxMarks = 10, ClassId = id, SubjectId = id, IsDraft = true },
                new Assignment { Id = id + 2, Title = "Other class", MaxMarks = 10, ClassId = id + 1, SubjectId = id });
            db.Classes.Add(new ClassEntity { Id = id + 1, Name = "Other", Section = "B" });
            return db.SaveChangesAsync();
        });
        SetIdentity(AppRoles.Student, student);

        var response = await _client.GetAsync("/api/dashboard/summary");
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.RootElement.GetProperty("availableAssignments").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task XmlExport_ReturnsValidRoleScopedAssignmentDto()
    {
        const int id = 3300;
        const string student = "xml-student@school.com";
        await SeedAcademicContextAsync(id, "xml-teacher@school.com", student);
        await SeedAsync(db =>
        {
            db.Assignments.Add(new Assignment
            {
                Id = id,
                Title = "XML assignment",
                Description = "Safe serialized DTO",
                MaxMarks = 25,
                ClassId = id,
                SubjectId = id
            });
            return db.SaveChangesAsync();
        });
        SetIdentity(AppRoles.Student, student);

        var response = await _client.GetAsync("/api/integrations/assignments/export?format=xml");
        var document = XDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");
        document.Root!.Name.LocalName.Should().Be("assignmentExport");
        document.Descendants("Title").Should().ContainSingle(element => element.Value == "XML assignment");
    }

    [Fact]
    public async Task AssignmentExport_RejectsUnsupportedFormat()
    {
        SetIdentity(AppRoles.Admin, "format-admin@school.com");

        var response = await _client.GetAsync("/api/integrations/assignments/export?format=yaml");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IntegrationFailure_DoesNotRollbackSuccessfulGrade()
    {
        const int id = 3400;
        const string teacher = "failure-teacher@school.com";
        const string student = "failure-student@school.com";
        await SeedAcademicContextAsync(id, teacher, student);
        await SeedAsync(db =>
        {
            db.Assignments.Add(new Assignment { Id = id, Title = "Grade safely", MaxMarks = 50, ClassId = id, SubjectId = id });
            db.Submissions.Add(new Submission { Id = id, AssignmentId = id, StudentEmail = student, AnswerText = "Answer" });
            return db.SaveChangesAsync();
        });
        _emails.ThrowOnSend = true;
        _webhooks.ThrowOnSend = true;
        SetIdentity(AppRoles.Teacher, teacher);

        var response = await _client.PutAsJsonAsync($"/api/submissions/{id}/grade", new { Marks = 45, Feedback = "Good" });
        var status = await ReadAsync(db => Task.FromResult(db.Submissions.Single(item => item.Id == id).Status));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        status.Should().Be(SubmissionStatuses.Graded);
    }

    [Fact]
    public async Task AssignmentFilters_AreAppliedBeforePagination()
    {
        const int id = 3500;
        await SeedAcademicContextAsync(id, "filter-teacher@school.com", "filter-student@school.com");
        await SeedAsync(db =>
        {
            db.Assignments.AddRange(
                new Assignment { Id = id, Title = "Database joins", MaxMarks = 10, ClassId = id, SubjectId = id },
                new Assignment { Id = id + 1, Title = "REST basics", MaxMarks = 10, ClassId = id, SubjectId = id, IsDraft = true });
            return db.SaveChangesAsync();
        });
        SetIdentity(AppRoles.Admin, "filter-admin@school.com");

        var response = await _client.GetAsync("/api/assignments?search=database&status=published&pageSize=10");
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.RootElement.GetProperty("totalItems").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("data")[0].GetProperty("title").GetString().Should().Be("Database joins");
    }

    private static void SetIdentity(string role, string email)
    {
        TestAuthHandler.CurrentRole = role;
        TestAuthHandler.CurrentEmail = email;
    }

    private Task SeedAcademicContextAsync(int id, string teacherEmail, string studentEmail) => SeedAsync(db =>
    {
        db.Classes.Add(new ClassEntity { Id = id, Name = $"Class {id}", Section = "A" });
        db.Subjects.Add(new Subject { Id = id, Name = $"Subject {id}", Code = $"P{id}" });
        db.Users.AddRange(
            new User { Id = id * 2, Email = teacherEmail, Role = AppRoles.Teacher, PasswordHash = "unused" },
            new User { Id = id * 2 + 1, Email = studentEmail, Role = AppRoles.Student, ClassId = id, PasswordHash = "unused" });
        db.TeacherAllocations.Add(new TeacherAllocation { Id = id, TeacherEmail = teacherEmail, ClassId = id, SubjectId = id });
        return db.SaveChangesAsync();
    });

    private async Task SeedAsync(Func<AppDbContext, Task> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private async Task<T> ReadAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
