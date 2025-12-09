using Microsoft.AspNetCore.Mvc;
using ResumeAI.Application.DTOs;
using ResumeAI.Application.Services;
using ResumeAI.Domain.Enums;

namespace ResumAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumeController : ControllerBase
{
    private readonly IResumeAnalysisService _analysisService;
    private readonly ILogger<ResumeController> _logger;

    public ResumeController(IResumeAnalysisService analysisService, ILogger<ResumeController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    /// <summary>
    /// LLM API key yapılandırması (Provider appsettings.json'dan alınır)
    /// </summary>
    [HttpPost("configure")]
    public IActionResult ConfigureLLM([FromBody] ConfigureLLMRequest request)
    {
        try
        {
            _analysisService.ConfigureLLM(request.ApiKey);
            var config = _analysisService.GetLLMConfig();
            _logger.LogInformation("LLM configured: {Provider}", config.Provider);
            return Ok(new { success = true, message = $"{config.Provider} başarıyla yapılandırıldı.", provider = config.Provider, model = config.Model });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM configuration failed");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// LLM durumunu kontrol eder
    /// </summary>
    [HttpGet("llm-status")]
    public IActionResult GetLLMStatus()
    {
        var status = _analysisService.GetLLMStatus();
        return Ok(status);
    }

    /// <summary>
    /// LLM yapılandırma bilgilerini getirir (appsettings.json'dan)
    /// </summary>
    [HttpGet("llm-config")]
    public IActionResult GetLLMConfig()
    {
        var config = _analysisService.GetLLMConfig();
        return Ok(config);
    }

    /// <summary>
    /// Yeni bir oturum oluşturur
    /// </summary>
    [HttpPost("session")]
    public IActionResult CreateSession()
    {
        var sessionId = _analysisService.CreateSession();
        _logger.LogInformation("Session created: {SessionId}", sessionId);
        return Ok(new { sessionId });
    }

    /// <summary>
    /// Oturumu siler
    /// </summary>
    [HttpDelete("session/{sessionId}")]
    public IActionResult ClearSession(Guid sessionId)
    {
        _analysisService.ClearSession(sessionId);
        _logger.LogInformation("Session cleared: {SessionId}", sessionId);
        return Ok(new { success = true, message = "Oturum temizlendi." });
    }

    /// <summary>
    /// Toplu CV yükleme
    /// </summary>
    [HttpPost("upload/{sessionId}")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    public async Task<IActionResult> UploadResumes(Guid sessionId, [FromForm] IFormFileCollection files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { success = false, message = "Dosya seçilmedi." });
            }

            _logger.LogInformation("Uploading {Count} files to session {SessionId}", files.Count, sessionId);
            var response = await _analysisService.UploadResumesAsync(sessionId, files);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for session {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Oturumdaki adayları listeler
    /// </summary>
    [HttpGet("candidates/{sessionId}")]
    public IActionResult GetCandidates(Guid sessionId)
    {
        var candidates = _analysisService.GetCandidates(sessionId);
        return Ok(candidates);
    }

    /// <summary>
    /// CV'leri iş gereksinimlerine göre analiz eder
    /// </summary>
    [HttpPost("analyze/{sessionId}")]
    public async Task<IActionResult> AnalyzeCandidates(Guid sessionId, [FromBody] AnalyzeRequest request)
    {
        try
        {
            _logger.LogInformation("Analyzing candidates for session {SessionId}", sessionId);
            var response = await _analysisService.AnalyzeCandidatesAsync(sessionId, request.JobRequirement);
            
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed for session {SessionId}", sessionId);
            return StatusCode(500, new { success = false, message = "Analiz sırasında beklenmeyen bir hata oluştu." });
        }
    }

    /// <summary>
    /// Analiz sonuçlarını getirir
    /// </summary>
    [HttpGet("results/{sessionId}")]
    public IActionResult GetResults(Guid sessionId)
    {
        var results = _analysisService.GetResults(sessionId);
        return Ok(results);
    }

    /// <summary>
    /// En iyi adayları getirir
    /// </summary>
    [HttpGet("top-candidates/{sessionId}")]
    public IActionResult GetTopCandidates(Guid sessionId, [FromQuery] int count = 10)
    {
        var results = _analysisService.GetTopCandidates(sessionId, count);
        return Ok(results);
    }
}
