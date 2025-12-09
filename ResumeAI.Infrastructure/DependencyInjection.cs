using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResumeAI.Domain.Interfaces;
using ResumeAI.Infrastructure.FileParser;
using ResumeAI.Infrastructure.LLM;

namespace ResumeAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // LLM Service - singleton to preserve configuration across requests
        services.AddHttpClient<ILLMService, LLMService>();
        
        // Auto-configure from appsettings if API key is provided
        services.AddSingleton<LLMConfigurationService>();
        
        // File Parser Service
        services.AddScoped<IFileParserService, FileParserService>();

        return services;
    }
}
