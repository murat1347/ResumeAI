namespace ResumeAI.Domain.Entities;

public class Education
{
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty; // Bachelor, Master, PhD, etc.
    public string FieldOfStudy { get; set; } = string.Empty;
    public int? GraduationYear { get; set; }
    public double? GPA { get; set; }
}
