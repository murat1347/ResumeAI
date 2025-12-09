namespace ResumeAI.Domain.Entities;

public class AnalysisResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    
    // Scores (0-100)
    public double SkillsScore { get; set; }
    public double ExperienceScore { get; set; }
    public double EducationScore { get; set; }
    
    // Weighted Total Score
    public double TotalScore { get; set; }
    
    // Score Breakdown Details
    public SkillsAnalysis? SkillsAnalysis { get; set; }
    public ExperienceAnalysis? ExperienceAnalysis { get; set; }
    public EducationAnalysis? EducationAnalysis { get; set; }
    
    // AI Response
    public string AISummary { get; set; } = string.Empty;
    public string Strengths { get; set; } = string.Empty;
    public string Weaknesses { get; set; } = string.Empty;
    
    // Candidate info extracted from analysis (when parsing failed)
    public string? CandidateName { get; set; }
    public string? CandidateEmail { get; set; }
    public string? CandidatePhone { get; set; }
    
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class SkillsAnalysis
{
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public int MatchedCount { get; set; }
    public int RequiredCount { get; set; }
}

public class ExperienceAnalysis
{
    public double TotalYearsOfExperience { get; set; }
    public int RequiredYears { get; set; }
    public double NumberOfCompanies { get; set; }
    public double AverageYearsPerCompany { get; set; }
    public bool HasRelevantExperience { get; set; }
}

public class EducationAnalysis
{
    public bool HasRequiredDegree { get; set; }
    public bool IsRelevantField { get; set; }
    public string ActualDegree { get; set; } = string.Empty;
    public string ActualField { get; set; } = string.Empty;
}
