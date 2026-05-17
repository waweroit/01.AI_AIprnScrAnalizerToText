using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIprnScrAnalizerToText;

public sealed class OllamaTextAgent : ITextAiAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Name => "OllamaText";

    public async Task<string> CompleteAsync(string prompt, AppSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.OllamaTextUrl)) throw new InvalidOperationException("Brak Ollama Text URL.");
        if (string.IsNullOrWhiteSpace(settings.OllamaTextModel)) throw new InvalidOperationException("Brak Ollama Text Model.");
        if (string.IsNullOrWhiteSpace(prompt)) throw new InvalidOperationException("Prompt dla Text LLM jest pusty.");

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(30, settings.RequestTimeoutSeconds))
        };

        var endpoint = settings.OllamaTextUrl.TrimEnd('/') + "/api/chat";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

        var payload = new
        {
            model = settings.OllamaTextModel.Trim(),
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            options = new
            {
                temperature = (double)settings.Temperature
            },
            stream = false
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Ollama Text zwróciła HTTP {(int)response.StatusCode}: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content))
        {
            var text = content.GetString();
            if (!string.IsNullOrWhiteSpace(text)) return text!;
        }

        return body;
    }
}
