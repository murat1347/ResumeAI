using ResumeAI.Domain.Enums;

namespace ResumeAI.Application.DTOs;

public class ConfigureLLMRequest
{
    public string ApiKey { get; set; } = string.Empty;
}

public class LLMStatusResponse
{
    public bool IsConfigured { get; set; }
    public string? CurrentProvider { get; set; }
    public string? CurrentModel { get; set; }
}

public class LLMConfigResponse
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
}
