namespace ResumeAI.Domain.Entities;

public class Experience
{
    public string CompanyName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    
    public int DurationInMonths => EndDate.HasValue 
        ? (int)((EndDate.Value - StartDate).TotalDays / 30)
        : (int)((DateTime.UtcNow - StartDate).TotalDays / 30);
}
