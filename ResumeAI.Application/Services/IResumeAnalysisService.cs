using Microsoft.AspNetCore.Http;
using ResumeAI.Application.DTOs;
using ResumeAI.Domain.Entities;
using ResumeAI.Domain.Enums;

namespace ResumeAI.Application.Services;

public interface IResumeAnalysisService
{
    // Session Management (in-memory storage)
    Guid CreateSession();
    void ClearSession(Guid sessionId);
    
    // LLM Configuration
    void ConfigureLLM(string apiKey);
    LLMStatusResponse GetLLMStatus();
    LLMConfigResponse GetLLMConfig();
    
    // Resume Operations
    Task<UploadResponse> UploadResumesAsync(Guid sessionId, IFormFileCollection files);
    List<CandidateDto> GetCandidates(Guid sessionId);
    
    // Analysis Operations
    Task<AnalyzeResponse> AnalyzeCandidatesAsync(Guid sessionId, JobRequirementDto requirement);
    List<AnalysisResultDto> GetResults(Guid sessionId);
    List<AnalysisResultDto> GetTopCandidates(Guid sessionId, int count = 10);
}
