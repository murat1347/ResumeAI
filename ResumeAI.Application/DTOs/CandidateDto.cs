using ResumeAI.Domain.Entities;

namespace ResumeAI.Application.DTOs;

public class CandidateDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    
    /// <summary>
    /// CV başarıyla parse edildiyse true
    /// </summary>
    public bool IsParsed { get; set; }
    
    public List<SkillDto> Skills { get; set; } = new();
    public List<ExperienceDto> Experiences { get; set; } = new();
    public EducationDto? Education { get; set; }
}

public class SkillDto
{
    public string Name { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string Level { get; set; } = string.Empty;
}

public class ExperienceDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public int DurationInMonths { get; set; }
}

public class EducationDto
{
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public int? GraduationYear { get; set; }
    public double? GPA { get; set; }
}
