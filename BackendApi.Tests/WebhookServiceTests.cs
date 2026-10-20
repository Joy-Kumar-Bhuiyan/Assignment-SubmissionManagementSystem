using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BackendApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BackendApi.Tests;

public sealed class WebhookServiceTests
{
    [Fact]
    public async Task WebhookService_SendsExpectedJsonPayload()
    {
        var handler = new RecordingHttpMessageHandler();
        var client = new HttpClient(handler);
        var options = Options.Create(new WebhookOptions
        {
            Enabled = true,
            Url = "https://receiver.example.test/events",
            Secret = "test-webhook-secret-at-least-32-bytes"
        });
        var service = new ConfiguredWebhookService(client, options);

        await service.SendAsync(new WebhookMessage(
            "submission.graded",
            new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc),
            new { submissionId = 7, assignmentId = 3 }));

        handler.RequestUri.Should().Be("https://receiver.example.test/events");
        var json = JsonDocument.Parse(handler.Body!);
        json.RootElement.GetProperty("event").GetString().Should().Be("submission.graded");
        json.RootElement.GetProperty("data").GetProperty("submissionId").GetInt32().Should().Be(7);
        var expectedSignature = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(options.Value.Secret),
            Encoding.UTF8.GetBytes(handler.Body!))).ToLowerInvariant();
        handler.Signature.Should().Be(expectedSignature);
    }

    [Fact]
    public async Task WebhookService_RetriesOnceForServerFailure()
    {
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        var service = CreateEnabledService(handler);

        await service.SendAsync(new WebhookMessage("submission.returned", DateTime.UtcNow, new { submissionId = 8 }));

        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task WebhookService_DoesNothingWhenDisabled()
    {
        var handler = new RecordingHttpMessageHandler();
        var service = new ConfiguredWebhookService(new HttpClient(handler), Options.Create(new WebhookOptions()));

        await service.SendAsync(new WebhookMessage("submission.graded", DateTime.UtcNow, new { submissionId = 9 }));

        handler.CallCount.Should().Be(0);
    }

    private static ConfiguredWebhookService CreateEnabledService(HttpMessageHandler handler) => new(
        new HttpClient(handler),
        Options.Create(new WebhookOptions
        {
            Enabled = true,
            Url = "https://receiver.example.test/events",
            Secret = "test-webhook-secret-at-least-32-bytes"
        }));

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _statuses;

        public RecordingHttpMessageHandler(params HttpStatusCode[] statuses)
        {
            _statuses = new Queue<HttpStatusCode>(statuses);
        }

        public string? RequestUri { get; private set; }
        public string? Body { get; private set; }
        public string? Signature { get; private set; }
        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            RequestUri = request.RequestUri?.ToString();
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Signature = request.Headers.GetValues("X-Webhook-Signature").Single();
            return new HttpResponseMessage(_statuses.Count > 0 ? _statuses.Dequeue() : HttpStatusCode.OK);
        }
    }
}
