namespace ResumeAI.Domain.Interfaces;

/// <summary>
/// Döküman parsing arayüzü - PDF, DOCX, Image dosyalarını text'e çevirir
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// Desteklenen dosya uzantıları
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }
    
    /// <summary>
    /// Dosyayı parse edip text çıkarır
    /// </summary>
    Task<DocumentParseResult> ParseAsync(byte[] fileContent, string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Belirtilen uzantıyı destekliyor mu?
    /// </summary>
    bool CanParse(string extension);
}

/// <summary>
/// Döküman parse sonucu
/// </summary>
public class DocumentParseResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExtractedText { get; set; }
    public int PageCount { get; set; }
}
