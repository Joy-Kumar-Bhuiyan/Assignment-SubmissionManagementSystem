using BackendApi.Data;
using BackendApi.Models;
using BackendApi.Security;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Services;

public interface INotificationService
{
    Task AssignmentPublishedAsync(Assignment assignment, CancellationToken cancellationToken);
    Task SubmissionCreatedAsync(Submission submission, CancellationToken cancellationToken);
    Task SubmissionGradedAsync(Submission submission, CancellationToken cancellationToken);
    Task SubmissionReturnedAsync(Submission submission, CancellationToken cancellationToken);
}

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IOutgoingWebhookService _webhook;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext db,
        IEmailService email,
        IOutgoingWebhookService webhook,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _email = email;
        _webhook = webhook;
        _logger = logger;
    }

    public async Task AssignmentPublishedAsync(Assignment assignment, CancellationToken cancellationToken)
    {
        var recipients = await _db.Users
            .AsNoTracking()
            .Where(user => user.Role == AppRoles.Student &&
                (assignment.ClassId == null || user.ClassId == assignment.ClassId))
            .Select(user => user.Email)
            .ToListAsync(cancellationToken);

        foreach (var recipient in recipients)
        {
            await TrySendEmailAsync(
                new EmailMessage(
                    recipient,
                    $"New assignment: {assignment.Title}",
                    $"A new assignment has been published. Due: {FormatDueDate(assignment.DueDate)}."),
                cancellationToken);
        }

        await TrySendWebhookAsync(new WebhookMessage(
            "assignment.published",
            DateTime.UtcNow,
            new { assignmentId = assignment.Id, assignment.ClassId, assignment.SubjectId }),
            cancellationToken);
    }

    public Task SubmissionCreatedAsync(Submission submission, CancellationToken cancellationToken) =>
        TrySendWebhookAsync(new WebhookMessage(
            "submission.created",
            DateTime.UtcNow,
            new { submissionId = submission.Id, submission.AssignmentId }),
            cancellationToken);

    public async Task SubmissionGradedAsync(Submission submission, CancellationToken cancellationToken)
    {
        await TrySendEmailAsync(
            new EmailMessage(
                submission.StudentEmail,
                "Your submission has been graded",
                "Your teacher has graded your submission. Sign in to view the result and feedback."),
            cancellationToken);

        await TrySendWebhookAsync(new WebhookMessage(
            "submission.graded",
            DateTime.UtcNow,
            new { submissionId = submission.Id, submission.AssignmentId }),
            cancellationToken);
    }

    public Task SubmissionReturnedAsync(Submission submission, CancellationToken cancellationToken) =>
        TrySendEmailAsync(
            new EmailMessage(
                submission.StudentEmail,
                "Your submission was returned for revision",
                "Your teacher returned your submission. Sign in to review it and submit a revision."),
            cancellationToken);

    private async Task TrySendEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await _email.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Email notification delivery failed for {Recipient}.", message.To);
        }
    }

    private async Task TrySendWebhookAsync(WebhookMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await _webhook.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Webhook delivery failed for event {Event}.", message.Event);
        }
    }

    private static string FormatDueDate(DateTime? dueDate) =>
        dueDate?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'") ?? "No deadline";
}
