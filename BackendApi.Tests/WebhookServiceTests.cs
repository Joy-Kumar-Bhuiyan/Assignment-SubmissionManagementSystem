using System.Net;
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
            Url = "https://receiver.example.test/events"
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
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
