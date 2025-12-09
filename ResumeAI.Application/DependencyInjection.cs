using Microsoft.Extensions.DependencyInjection;
using ResumeAI.Application.Services;

namespace ResumeAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IResumeAnalysisService, ResumeAnalysisService>();
        
        return services;
    }
}
