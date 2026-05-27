using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace ServiceLayer.Services;

public class TextExtractor : ITextExtractor
{
    public List<(int Page, string Text)> Extract(Stream stream, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext == ".pdf" || contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            return ExtractPdf(stream);

        if (ext == ".docx" || contentType.Contains("officedocument.wordprocessingml", StringComparison.OrdinalIgnoreCase))
            return ExtractDocx(stream);

        // Plain text fallback
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        return new List<(int, string)> { (1, text) };
    }

    private static List<(int Page, string Text)> ExtractPdf(Stream stream)
    {
        var pages = new List<(int, string)>();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        using var pdf = PdfDocument.Open(ms);
        int i = 1;
        foreach (var p in pdf.GetPages())
        {
            pages.Add((i, p.Text ?? string.Empty));
            i++;
        }
        return pages;
    }

    private static List<(int Page, string Text)> ExtractDocx(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        using var word = WordprocessingDocument.Open(ms, false);
        var body = word.MainDocumentPart?.Document.Body;
        if (body is null) return new List<(int, string)> { (1, string.Empty) };

        var paragraphs = body.Descendants<Paragraph>().Select(p => p.InnerText);
        var text = string.Join("\n", paragraphs);
        return new List<(int, string)> { (1, text) };
    }
}
