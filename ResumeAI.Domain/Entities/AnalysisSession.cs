namespace ResumeAI.Domain.Entities;

/// <summary>
/// CV analiz oturumunu temsil eden entity
/// Kullanıcı birden fazla CV yükleyip analiz başlattığında bir session oluşturulur
/// </summary>
public class AnalysisSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;
    
    // İş gereksinimleri
    public JobRequirement? JobRequirement { get; set; }
    
    // LLM Provider bilgileri
    public string LlmProvider { get; set; } = "OpenAI"; // OpenAI, Gemini, Qwen
    public string LlmModel { get; set; } = "gpt-4";
    
    // Navigation
    public List<Candidate> Candidates { get; set; } = new();
}

public enum AnalysisStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
