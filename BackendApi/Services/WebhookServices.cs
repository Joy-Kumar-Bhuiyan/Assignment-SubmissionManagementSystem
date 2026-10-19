using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace BackendApi.Services;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhook";

    public bool Enabled { get; init; }
    public string Url { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 5;
}

public sealed record WebhookMessage(string Event, DateTime OccurredAtUtc, object Data);

public interface IOutgoingWebhookService
{
    Task SendAsync(WebhookMessage message, CancellationToken cancellationToken = default);
}

public sealed class ConfiguredWebhookService : IOutgoingWebhookService
{
    private readonly HttpClient _client;
    private readonly WebhookOptions _options;

    public ConfiguredWebhookService(HttpClient client, IOptions<WebhookOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task SendAsync(WebhookMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        using var response = await _client.PostAsJsonAsync(_options.Url, message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
