namespace ResumeAI.Application.DTOs;

public class AnalyzeRequest
{
    public JobRequirementDto JobRequirement { get; set; } = new();
}

public class AnalyzeResponse
{
    public Guid SessionId { get; set; }
    public int TotalCandidates { get; set; }
    public int SuccessfullyAnalyzed { get; set; }
    public int FailedToAnalyze { get; set; }
    public List<AnalysisResultDto> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}

public class UploadResponse
{
    public Guid SessionId { get; set; }
    public int TotalFiles { get; set; }
    public int SuccessfullyUploaded { get; set; }
    public int FailedToUpload { get; set; }
    public List<CandidateDto> Candidates { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
