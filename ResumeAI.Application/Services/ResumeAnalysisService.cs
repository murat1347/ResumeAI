using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ResumeAI.Application.DTOs;
using ResumeAI.Application.Mapping;
using ResumeAI.Domain.Entities;
using ResumeAI.Domain.Enums;
using ResumeAI.Domain.Interfaces;

namespace ResumeAI.Application.Services;

public class ResumeAnalysisService : IResumeAnalysisService
{
    private readonly ILLMService _llmService;
    private readonly IFileParserService _fileParserService;
    private readonly LLMSettings _llmSettings;
    
    // In-memory session storage
    private static readonly ConcurrentDictionary<Guid, SessionData> _sessions = new();

    public ResumeAnalysisService(ILLMService llmService, IFileParserService fileParserService, IOptions<LLMSettings> llmSettings)
    {
        _llmService = llmService;
        _fileParserService = fileParserService;
        _llmSettings = llmSettings.Value;
        
        // Eğer appsettings'de API key varsa otomatik yapılandır
        if (!string.IsNullOrEmpty(_llmSettings.ApiKey))
        {
            ConfigureLLM(_llmSettings.ApiKey);
        }
    }

    public Guid CreateSession()
    {
        var sessionId = Guid.NewGuid();
        _sessions[sessionId] = new SessionData();
        return sessionId;
    }

    public void ClearSession(Guid sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    public void ConfigureLLM(string apiKey)
    {
        var provider = Enum.Parse<LLMProvider>(_llmSettings.Provider, true);
        _llmService.Configure(provider, apiKey);
    }

    public LLMStatusResponse GetLLMStatus()
    {
        return new LLMStatusResponse
        {
            IsConfigured = _llmService.IsConfigured,
            CurrentProvider = _llmService.IsConfigured ? _llmService.CurrentProvider.ToString() : _llmSettings.Provider,
            CurrentModel = _llmSettings.Models.TryGetValue(_llmSettings.Provider, out var model) ? model : null
        };
    }

    public LLMConfigResponse GetLLMConfig()
    {
        return new LLMConfigResponse
        {
            Provider = _llmSettings.Provider,
            Model = _llmSettings.Models.TryGetValue(_llmSettings.Provider, out var model) ? model : "",
            HasApiKey = !string.IsNullOrEmpty(_llmSettings.ApiKey)
        };
    }

    public async Task<UploadResponse> UploadResumesAsync(Guid sessionId, IFormFileCollection files)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            session = new SessionData();
            _sessions[sessionId] = session;
        }

        var response = new UploadResponse
        {
            SessionId = sessionId,
            TotalFiles = files.Count
        };

        foreach (var file in files)
        {
            try
            {
                if (!_fileParserService.IsSupported(file.FileName))
                {
                    response.Errors.Add($"Desteklenmeyen dosya formatı: {file.FileName}. Desteklenen formatlar: {string.Join(", ", _fileParserService.SupportedExtensions)}");
                    response.FailedToUpload++;
                    continue;
                }

                using var stream = file.OpenReadStream();
                var content = await _fileParserService.ExtractTextAsync(stream, file.FileName);

                if (string.IsNullOrWhiteSpace(content))
                {
                    response.Errors.Add($"Dosyadan metin çıkarılamadı: {file.FileName}");
                    response.FailedToUpload++;
                    continue;
                }

                // Parse resume with LLM if configured, otherwise create basic candidate
                Candidate candidate;
                if (_llmService.IsConfigured)
                {
                    candidate = await _llmService.ParseResumeAsync(content, file.FileName);
                }
                else
                {
                    candidate = new Candidate
                    {
                        FileName = file.FileName,
                        RawContent = content
                    };
                }

                session.Candidates.Add(candidate);
                response.Candidates.Add(candidate.ToDto());
                response.SuccessfullyUploaded++;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Dosya işlenirken hata oluştu ({file.FileName}): {ex.Message}");
                response.FailedToUpload++;
            }
        }

        return response;
    }

    public List<CandidateDto> GetCandidates(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return new List<CandidateDto>();

        return session.Candidates.Select(c => c.ToDto()).ToList();
    }

    public async Task<AnalyzeResponse> AnalyzeCandidatesAsync(Guid sessionId, JobRequirementDto requirementDto)
    {
        if (!_llmService.IsConfigured)
        {
            throw new InvalidOperationException("LLM servisi yapılandırılmamış. Lütfen önce API anahtarı girin.");
        }

        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException("Oturum bulunamadı. Lütfen önce CV yükleyin.");
        }

        if (!session.Candidates.Any())
        {
            throw new InvalidOperationException("Analiz edilecek CV bulunamadı. Lütfen önce CV yükleyin.");
        }

        var requirement = requirementDto.ToEntity();
        var response = new AnalyzeResponse
        {
            SessionId = sessionId,
            TotalCandidates = session.Candidates.Count,
            AnalyzedAt = DateTime.UtcNow
        };

        // Clear previous results
        session.Results.Clear();

        foreach (var candidate in session.Candidates)
        {
            try
            {
                var result = await _llmService.AnalyzeCandidateAsync(candidate, requirement);
                candidate.AnalysisResult = result;
                session.Results.Add(result);
                response.Results.Add(result.ToDto(candidate));
                response.SuccessfullyAnalyzed++;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Aday analiz edilirken hata oluştu ({candidate.FullName ?? candidate.FileName}): {ex.Message}");
                response.FailedToAnalyze++;
            }
        }

        // Sort results by total score descending
        response.Results = response.Results.OrderByDescending(r => r.TotalScore).ToList();

        return response;
    }

    public List<AnalysisResultDto> GetResults(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return new List<AnalysisResultDto>();

        return session.Candidates
            .Where(c => c.AnalysisResult != null)
            .Select(c => c.AnalysisResult!.ToDto(c))
            .OrderByDescending(r => r.TotalScore)
            .ToList();
    }

    public List<AnalysisResultDto> GetTopCandidates(Guid sessionId, int count = 10)
    {
        return GetResults(sessionId).Take(count).ToList();
    }

    private class SessionData
    {
        public List<Candidate> Candidates { get; } = new();
        public List<AnalysisResult> Results { get; } = new();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }
}
