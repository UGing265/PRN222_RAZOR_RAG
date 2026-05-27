using BLL.Interfaces.Documents;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using System.Text;
using A = DocumentFormat.OpenXml.Drawing;
using WText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace BLL.Services.Documents;

public class SimpleFileParserService : IFileParserService
{
    private readonly ILogger<SimpleFileParserService> _logger;

    public SimpleFileParserService(ILogger<SimpleFileParserService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(string filePath, string extension, CancellationToken cancellationToken = default)
    {
        extension = extension.ToLowerInvariant();

        return extension switch
        {
            ".txt" or ".md" => await File.ReadAllTextAsync(filePath, cancellationToken),
            ".pdf" => ExtractPdfText(filePath),
            ".docx" => ExtractDocxText(filePath),
            ".pptx" => ExtractPptxText(filePath),
            ".doc" => ExtractDocText(filePath),
            _ => string.Empty
        };
    }

    private string ExtractPdfText(string filePath)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page);
            sb.AppendLine(text);
        }

        return sb.ToString();
    }

    private string ExtractDocxText(string filePath)
    {
        var sb = new StringBuilder();

        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null)
        {
            return string.Empty;
        }

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var text = string.Concat(paragraph.Descendants<WText>().Select(t => t.Text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }

        return sb.ToString();
    }

    private string ExtractPptxText(string filePath)
    {
        var sb = new StringBuilder();

        using var doc = PresentationDocument.Open(filePath, false);
        var presentation = doc.PresentationPart?.Presentation;
        if (presentation?.SlideIdList is null)
        {
            return string.Empty;
        }

        foreach (var slideId in presentation.SlideIdList.Elements<SlideId>())
        {
            var slidePart = doc.PresentationPart?.GetPartById(slideId.RelationshipId!) as SlidePart;
            if (slidePart?.Slide is null)
            {
                continue;
            }

            foreach (var text in slidePart.Slide.Descendants<A.Text>())
            {
                if (!string.IsNullOrWhiteSpace(text.Text))
                {
                    sb.AppendLine(text.Text);
                }
            }
        }

        return sb.ToString();
    }

    private string ExtractDocText(string filePath)
    {
        throw new NotSupportedException(".doc requires the XWPF parser package or conversion to .docx in this setup.");
    }
}
