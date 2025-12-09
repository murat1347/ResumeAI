using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ResumeAI.Domain.Interfaces;

namespace ResumeAI.Infrastructure.FileParser;

/// <summary>
/// PDF, DOCX ve diğer dosya formatlarını text'e çeviren servis
/// </summary>
public class FileParserService : IFileParserService
{
    public string[] SupportedExtensions => new[] { ".pdf", ".docx", ".doc", ".txt" };

    public bool IsSupported(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => await ExtractFromPdfAsync(fileStream),
            ".docx" => await ExtractFromDocxAsync(fileStream),
            ".doc" => await ExtractFromDocAsync(fileStream),
            ".txt" => await ExtractFromTxtAsync(fileStream),
            _ => throw new NotSupportedException($"Desteklenmeyen dosya formatı: {extension}")
        };
    }

    private async Task<string> ExtractFromPdfAsync(Stream fileStream)
    {
        return await Task.Run(() =>
        {
            var text = new StringBuilder();

            using var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            using var pdfReader = new PdfReader(memoryStream);
            using var pdfDoc = new PdfDocument(pdfReader);

            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                var strategy = new SimpleTextExtractionStrategy();
                var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                text.AppendLine(pageText);
            }

            return text.ToString();
        });
    }

    private async Task<string> ExtractFromDocxAsync(Stream fileStream)
    {
        return await Task.Run(() =>
        {
            var text = new StringBuilder();

            using var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            using var wordDoc = WordprocessingDocument.Open(memoryStream, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;

            if (body != null)
            {
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    text.AppendLine(paragraph.InnerText);
                }

                // Tabloları da oku
                foreach (var table in body.Elements<Table>())
                {
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var cellTexts = row.Elements<TableCell>()
                            .Select(c => c.InnerText);
                        text.AppendLine(string.Join(" | ", cellTexts));
                    }
                }
            }

            return text.ToString();
        });
    }

    private async Task<string> ExtractFromDocAsync(Stream fileStream)
    {
        // .doc formatı için basit bir yaklaşım
        // Gerçek projede daha gelişmiş bir kütüphane kullanılabilir
        return await ExtractFromTxtAsync(fileStream);
    }

    private async Task<string> ExtractFromTxtAsync(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8, true);
        return await reader.ReadToEndAsync();
    }
}
