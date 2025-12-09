namespace ResumeAI.Application.DTOs;

public class LLMSettings
{
    public string Provider { get; set; } = "Gemini";
    public string ApiKey { get; set; } = string.Empty;
    public Dictionary<string, string> Models { get; set; } = new();
}
