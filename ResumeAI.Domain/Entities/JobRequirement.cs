namespace ResumeAI.Domain.Entities;

public class JobRequirement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string JobTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Required Skills
    public List<string> RequiredSkills { get; set; } = new();
    public List<string> PreferredSkills { get; set; } = new();
    
    // Experience Requirements
    public int MinYearsOfExperience { get; set; }
    public int? MaxYearsOfExperience { get; set; }
    
    // Education Requirements
    public string RequiredDegree { get; set; } = string.Empty; // High School, Bachelor, Master, PhD
    public List<string> PreferredFieldsOfStudy { get; set; } = new();
    
    // Scoring Weights (must sum to 100)
    public int SkillsWeight { get; set; } = 40;
    public int ExperienceWeight { get; set; } = 40;
    public int EducationWeight { get; set; } = 20;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
