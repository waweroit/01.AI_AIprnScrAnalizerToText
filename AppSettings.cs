using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIprnScrAnalizerToText;

public sealed class AppSettings
{
    public string SelectedVisionAgent { get; set; } = "OllamaVision";
    public string SelectedTextAgent { get; set; } = "OllamaText";

    public string OllamaVisionUrl { get; set; } = "http://localhost:11434";
    public string OllamaVisionModel { get; set; } = "qwen3-vl:8b";

    public string OllamaTextUrl { get; set; } = "http://localhost:11434";
    public string OllamaTextModel { get; set; } = "qwen3:8b";

    public string OpenAIApiKey { get; set; } = string.Empty;
    public string OpenAIBaseUrl { get; set; } = "https://api.openai.com/v1";
    public string OpenAIVisionModel { get; set; } = "gpt-4o-mini";
    public string OpenAITextModel { get; set; } = "gpt-4o-mini";

    public decimal Temperature { get; set; } = 0.2m;
    public int RequestTimeoutSeconds { get; set; } = 300;

    public string VisionPrompt { get; set; } = "Opisz dokładnie, co widzisz na zrzucie ekranu. Odczytaj widoczny tekst, zachowaj numerację, litery odpowiedzi i ważne elementy interfejsu.";

    public string TextAgentPrompt { get; set; } = "Na podstawie opisu z modelu Vision wykonaj zadanie użytkownika. Jeżeli brakuje informacji, napisz wprost, że nie da się ustalić odpowiedzi. Nie halucynuj. Odpowiadaj krótko i konkretnie.";

    public bool IncludeVisionPromptInFinalContext { get; set; } = false;

    [JsonIgnore]
    public static string SettingsFilePath => Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions()) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions());
        File.WriteAllText(SettingsFilePath, json);
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
