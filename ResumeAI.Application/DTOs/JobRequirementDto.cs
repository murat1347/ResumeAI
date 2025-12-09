namespace ResumeAI.Application.DTOs;

public class JobRequirementDto
{
    public string JobTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Required Skills (comma-separated or list)
    public List<string> RequiredSkills { get; set; } = new();
    public List<string> PreferredSkills { get; set; } = new();
    
    // Experience Requirements
    public int MinYearsOfExperience { get; set; }
    public int? MaxYearsOfExperience { get; set; }
    
    // Education Requirements
    public string RequiredDegree { get; set; } = string.Empty;
    public List<string> PreferredFieldsOfStudy { get; set; } = new();
    
    // Scoring Weights
    public int SkillsWeight { get; set; } = 40;
    public int ExperienceWeight { get; set; } = 40;
    public int EducationWeight { get; set; } = 20;
}
