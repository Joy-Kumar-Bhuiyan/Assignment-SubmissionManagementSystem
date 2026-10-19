using System.Collections.Concurrent;
using BackendApi.Services;

namespace BackendApi.Tests;

public sealed class RecordingEmailService : IEmailService
{
    public ConcurrentQueue<EmailMessage> Messages { get; } = new();
    public bool ThrowOnSend { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSend) throw new InvalidOperationException("Simulated email provider failure.");
        Messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        Messages.Clear();
        ThrowOnSend = false;
    }
}

public sealed class RecordingWebhookService : IOutgoingWebhookService
{
    public ConcurrentQueue<WebhookMessage> Messages { get; } = new();
    public bool ThrowOnSend { get; set; }

    public Task SendAsync(WebhookMessage message, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSend) throw new HttpRequestException("Simulated webhook receiver failure.");
        Messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        Messages.Clear();
        ThrowOnSend = false;
    }
}
