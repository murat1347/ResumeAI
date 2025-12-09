namespace ResumeAI.Domain.Interfaces;

public interface IFileParserService
{
    Task<string> ExtractTextAsync(Stream fileStream, string fileName);
    bool IsSupported(string fileName);
    string[] SupportedExtensions { get; }
}
