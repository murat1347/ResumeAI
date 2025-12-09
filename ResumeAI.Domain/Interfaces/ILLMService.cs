using ResumeAI.Domain.Entities;
using ResumeAI.Domain.Enums;

namespace ResumeAI.Domain.Interfaces;

public interface ILLMService
{
    Task<Candidate> ParseResumeAsync(string content, string fileName);
    Task<AnalysisResult> AnalyzeCandidateAsync(Candidate candidate, JobRequirement requirement);
    void Configure(LLMProvider provider, string apiKey);
    LLMProvider CurrentProvider { get; }
    bool IsConfigured { get; }
}
