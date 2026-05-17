using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIprnScrAnalizerToText;

public sealed class OpenAITextAgent : ITextAiAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Name => "OpenAIText";

    public async Task<string> CompleteAsync(string prompt, AppSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.OpenAIApiKey)) throw new InvalidOperationException("Brak OpenAI ApiKey.");
        if (string.IsNullOrWhiteSpace(settings.OpenAIBaseUrl)) throw new InvalidOperationException("Brak OpenAI Base URL.");
        if (string.IsNullOrWhiteSpace(settings.OpenAITextModel)) throw new InvalidOperationException("Brak OpenAI Text Model.");
        if (string.IsNullOrWhiteSpace(prompt)) throw new InvalidOperationException("Prompt dla Text LLM jest pusty.");

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(30, settings.RequestTimeoutSeconds))
        };

        var endpoint = settings.OpenAIBaseUrl.TrimEnd('/') + "/chat/completions";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenAIApiKey.Trim());

        var payload = new
        {
            model = settings.OpenAITextModel.Trim(),
            messages = new object[]
            {
                new { role = "user", content = prompt }
            },
            temperature = (double)settings.Temperature
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI Text zwrócił HTTP {(int)response.StatusCode}: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return string.IsNullOrWhiteSpace(text) ? body : text;
    }
}
