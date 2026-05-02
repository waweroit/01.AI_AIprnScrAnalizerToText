using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIprnScrAnalizerToText;

public sealed class OllamaVisionAgent : IAiAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(5) };

    public string Name => "OllamaVision";

    public async Task<string> AnalyzeImageAsync(byte[] imageBytes, string prompt, AppSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.OllamaVisionUrl)) throw new InvalidOperationException("Brak OllamaVision URL.");
        if (string.IsNullOrWhiteSpace(settings.OllamaVisionModel)) throw new InvalidOperationException("Brak OllamaVision Model.");
        if (imageBytes is null || imageBytes.Length == 0) throw new ArgumentException("Obraz jest pusty.", nameof(imageBytes));

        var endpoint = settings.OllamaVisionUrl.TrimEnd('/') + "/api/chat";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

        var payload = new
        {
            model = settings.OllamaVisionModel.Trim(),
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = prompt,
                    images = new[] { Convert.ToBase64String(imageBytes) }
                }
            },
            stream = false
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
        {
            var text = content.GetString();
            if (!string.IsNullOrWhiteSpace(text)) return text!;
        }

        return body;
    }
}
