namespace ResumeAI.Domain.Entities;

public class Candidate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// CV başarıyla parse edildiyse true, aksi halde false
    /// Parse edilemeyen CV'ler analiz sırasında işlenir
    /// </summary>
    public bool IsParsed { get; set; } = true;
    
    // Parsed CV Data
    public List<Skill> Skills { get; set; } = new();
    public List<Experience> Experiences { get; set; } = new();
    public Education? Education { get; set; }
    
    // Analysis Result
    public AnalysisResult? AnalysisResult { get; set; }
}
