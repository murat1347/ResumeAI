namespace ResumeAI.Domain.Entities;

public class Skill
{
    public string Name { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string Level { get; set; } = string.Empty; // Beginner, Intermediate, Advanced, Expert
}
