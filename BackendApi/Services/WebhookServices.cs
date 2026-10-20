using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BackendApi.Services;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhook";

    public bool Enabled { get; init; }
    public string Url { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
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

        var body = JsonSerializer.Serialize(message, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var signatureBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.Secret), Encoding.UTF8.GetBytes(body));
        var signature = $"sha256={Convert.ToHexString(signatureBytes).ToLowerInvariant()}";

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Webhook-Signature", signature);

            try
            {
                using var response = await _client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode) return;

                var retryable = response.StatusCode == HttpStatusCode.RequestTimeout ||
                                (int)response.StatusCode == 429 ||
                                (int)response.StatusCode >= 500;
                if (!retryable || attempt == 2) response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException) when (attempt == 1)
            {
                // One immediate retry keeps the integration small; durable delivery needs an outbox.
            }
        }
    }
}
