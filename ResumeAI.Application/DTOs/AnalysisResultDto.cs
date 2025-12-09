namespace ResumeAI.Application.DTOs;

public class AnalysisResultDto
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public CandidateDto? Candidate { get; set; }
    
    // Scores (0-100)
    public double SkillsScore { get; set; }
    public double ExperienceScore { get; set; }
    public double EducationScore { get; set; }
    public double TotalScore { get; set; }
    
    // Score Details
    public SkillsAnalysisDto? SkillsAnalysis { get; set; }
    public ExperienceAnalysisDto? ExperienceAnalysis { get; set; }
    public EducationAnalysisDto? EducationAnalysis { get; set; }
    
    // AI Analysis
    public string AISummary { get; set; } = string.Empty;
    public string Strengths { get; set; } = string.Empty;
    public string Weaknesses { get; set; } = string.Empty;
    
    public DateTime AnalyzedAt { get; set; }
}

public class SkillsAnalysisDto
{
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public int MatchedCount { get; set; }
    public int RequiredCount { get; set; }
    public double MatchPercentage => RequiredCount > 0 ? (double)MatchedCount / RequiredCount * 100 : 0;
}

public class ExperienceAnalysisDto
{
    public double TotalYearsOfExperience { get; set; }
    public int RequiredYears { get; set; }
    public double NumberOfCompanies { get; set; }
    public double AverageYearsPerCompany { get; set; }
    public bool HasRelevantExperience { get; set; }
}

public class EducationAnalysisDto
{
    public bool HasRequiredDegree { get; set; }
    public bool IsRelevantField { get; set; }
    public string ActualDegree { get; set; } = string.Empty;
    public string ActualField { get; set; } = string.Empty;
}
