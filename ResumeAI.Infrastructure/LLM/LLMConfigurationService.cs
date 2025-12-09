using Microsoft.Extensions.Configuration;
using ResumeAI.Domain.Enums;
using ResumeAI.Domain.Interfaces;

namespace ResumeAI.Infrastructure.LLM;

/// <summary>
/// LLM konfigürasyonunu yöneten servis - appsettings.json'dan okur
/// </summary>
public class LLMConfigurationService
{
    private readonly IConfiguration _configuration;

    public LLMConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LLMProvider GetConfiguredProvider()
    {
        var providerStr = _configuration["LLMSettings:Provider"] ?? "OpenAI";
        return Enum.TryParse<LLMProvider>(providerStr, true, out var provider) 
            ? provider 
            : LLMProvider.OpenAI;
    }

    public string GetApiKey()
    {
        return _configuration["LLMSettings:ApiKey"] ?? "";
    }

    public string GetModel(LLMProvider provider)
    {
        var modelKey = $"LLMSettings:Models:{provider}";
        return _configuration[modelKey] ?? GetDefaultModel(provider);
    }

    private string GetDefaultModel(LLMProvider provider)
    {
        return provider switch
        {
            LLMProvider.OpenAI => "gpt-4o-mini",
            LLMProvider.Gemini => "gemini-1.5-flash",
            LLMProvider.Qwen => "qwen-turbo",
            _ => "gpt-4o-mini"
        };
    }
}
