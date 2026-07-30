using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace PolicyProof.Services;

public interface ITextExtractorService
{
    Task<string> ExtractTextAsync(IFormFile file);
}

public class TextExtractorService : ITextExtractorService
{
    public async Task<string> ExtractTextAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".md" => await ExtractFromTextFile(file),
            ".docx" => ExtractFromDocx(file),
            ".pdf" => ExtractFromPdf(file),
            _ => throw new NotSupportedException($"File type '{extension}' is not supported. Upload .txt, .docx, or .pdf.")
        };
    }

    private static async Task<string> ExtractFromTextFile(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }

    private static string ExtractFromDocx(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        return body?.InnerText ?? string.Empty;
    }

    private static string ExtractFromPdf(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);
        var pages = document.GetPages();
        var text = string.Join("\n\n", pages.Select((p, i) => $"[Page {i + 1}]\n{p.Text}"));

        if (text.Length < 100 && document.NumberOfPages > 1)
            throw new InvalidOperationException("This PDF appears to be scanned/image-based. Please upload a text-based PDF or .docx file.");

        return text;
    }
}
