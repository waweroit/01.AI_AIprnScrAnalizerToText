namespace AIprnScrAnalizerToText;

public sealed class AppSettings
{
    public string SelectedAgent { get; set; } = "OpenAICompatible";
    public string OllamaVisionUrl { get; set; } = "http://78.9.232.15:11434";
    public string OllamaVisionModel { get; set; } = "qwen3-vl:8b";
    public string OpenAIApiKey { get; set; } = "sk-proj-XXXXXXXXXXXXXXXXXXXX";
    public string OpenAIBaseUrl { get; set; } = "https://api.openai.com/v1";
}
