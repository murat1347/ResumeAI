using ResumeAI.Application.DTOs;
using ResumeAI.Domain.Entities;

namespace ResumeAI.Application.Mapping;

public static class MappingExtensions
{
    public static CandidateDto ToDto(this Candidate candidate)
    {
        return new CandidateDto
        {
            Id = candidate.Id,
            FullName = candidate.FullName,
            Email = candidate.Email,
            Phone = candidate.Phone,
            FileName = candidate.FileName,
            UploadedAt = candidate.UploadedAt,
            Skills = candidate.Skills.Select(s => s.ToDto()).ToList(),
            Experiences = candidate.Experiences.Select(e => e.ToDto()).ToList(),
            Education = candidate.Education?.ToDto()
        };
    }

    public static SkillDto ToDto(this Skill skill)
    {
        return new SkillDto
        {
            Name = skill.Name,
            YearsOfExperience = skill.YearsOfExperience,
            Level = skill.Level
        };
    }

    public static ExperienceDto ToDto(this Experience experience)
    {
        return new ExperienceDto
        {
            CompanyName = experience.CompanyName,
            Position = experience.Position,
            StartDate = experience.StartDate,
            EndDate = experience.EndDate,
            Description = experience.Description,
            IsCurrent = experience.IsCurrent,
            DurationInMonths = experience.DurationInMonths
        };
    }

    public static EducationDto ToDto(this Education education)
    {
        return new EducationDto
        {
            Institution = education.Institution,
            Degree = education.Degree,
            FieldOfStudy = education.FieldOfStudy,
            GraduationYear = education.GraduationYear,
            GPA = education.GPA
        };
    }

    public static AnalysisResultDto ToDto(this AnalysisResult result, Candidate? candidate = null)
    {
        return new AnalysisResultDto
        {
            Id = result.Id,
            CandidateId = result.CandidateId,
            Candidate = candidate?.ToDto(),
            SkillsScore = result.SkillsScore,
            ExperienceScore = result.ExperienceScore,
            EducationScore = result.EducationScore,
            TotalScore = result.TotalScore,
            SkillsAnalysis = result.SkillsAnalysis?.ToDto(),
            ExperienceAnalysis = result.ExperienceAnalysis?.ToDto(),
            EducationAnalysis = result.EducationAnalysis?.ToDto(),
            AISummary = result.AISummary,
            Strengths = result.Strengths,
            Weaknesses = result.Weaknesses,
            AnalyzedAt = result.AnalyzedAt
        };
    }

    public static SkillsAnalysisDto ToDto(this SkillsAnalysis analysis)
    {
        return new SkillsAnalysisDto
        {
            MatchedSkills = analysis.MatchedSkills,
            MissingSkills = analysis.MissingSkills,
            MatchedCount = analysis.MatchedCount,
            RequiredCount = analysis.RequiredCount
        };
    }

    public static ExperienceAnalysisDto ToDto(this ExperienceAnalysis analysis)
    {
        return new ExperienceAnalysisDto
        {
            TotalYearsOfExperience = analysis.TotalYearsOfExperience,
            RequiredYears = analysis.RequiredYears,
            NumberOfCompanies = analysis.NumberOfCompanies,
            AverageYearsPerCompany = analysis.AverageYearsPerCompany,
            HasRelevantExperience = analysis.HasRelevantExperience
        };
    }

    public static EducationAnalysisDto ToDto(this EducationAnalysis analysis)
    {
        return new EducationAnalysisDto
        {
            HasRequiredDegree = analysis.HasRequiredDegree,
            IsRelevantField = analysis.IsRelevantField,
            ActualDegree = analysis.ActualDegree,
            ActualField = analysis.ActualField
        };
    }

    public static JobRequirement ToEntity(this JobRequirementDto dto)
    {
        return new JobRequirement
        {
            JobTitle = dto.JobTitle,
            Description = dto.Description,
            RequiredSkills = dto.RequiredSkills,
            PreferredSkills = dto.PreferredSkills,
            MinYearsOfExperience = dto.MinYearsOfExperience,
            MaxYearsOfExperience = dto.MaxYearsOfExperience,
            RequiredDegree = dto.RequiredDegree,
            PreferredFieldsOfStudy = dto.PreferredFieldsOfStudy,
            SkillsWeight = dto.SkillsWeight,
            ExperienceWeight = dto.ExperienceWeight,
            EducationWeight = dto.EducationWeight
        };
    }
}
