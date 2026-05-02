using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIprnScrAnalizerToText;

public sealed class OpenAICompatibleAgent : IAiAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    private readonly HttpClient _httpClient = new();
    public string Name => "OpenAICompatible";

    public async Task<string> AnalyzeImageAsync(byte[] imageBytes, string prompt, AppSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.OpenAIApiKey)) throw new InvalidOperationException("Brak OpenAI ApiKey.");
        if (string.IsNullOrWhiteSpace(settings.OpenAIBaseUrl)) throw new InvalidOperationException("Brak OpenAI Base URL.");
        if (imageBytes is null || imageBytes.Length == 0) throw new ArgumentException("Obraz jest pusty.", nameof(imageBytes));

        var endpoint = settings.OpenAIBaseUrl.TrimEnd('/') + "/chat/completions";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenAIApiKey.Trim());

        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new { role = "user", content = new object[] { new { type = "text", text = prompt }, new { type = "image_url", image_url = new { url = "data:image/png;base64," + Convert.ToBase64String(imageBytes) } } } }
            },
            temperature = 0.2
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return string.IsNullOrWhiteSpace(text) ? body : text;
    }
}
