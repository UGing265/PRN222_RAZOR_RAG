using BLL.Interfaces.Documents;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Spire.Doc;
using UglyToad.PdfPig;
using System.Text;
using A = DocumentFormat.OpenXml.Drawing;
using WText = DocumentFormat.OpenXml.Wordprocessing.Text;
using WBreak = DocumentFormat.OpenXml.Wordprocessing.Break;

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
        var pages = await ExtractPagesAsync(filePath, extension, cancellationToken);
        var sb = new StringBuilder();
        foreach (var page in pages)
        {
            sb.AppendLine(page.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public async Task<List<PageContent>> ExtractPagesAsync(string filePath, string extension, CancellationToken cancellationToken = default)
    {
        extension = extension.ToLowerInvariant();
        var pages = new List<PageContent>();

        switch (extension)
        {
            case ".txt" or ".md":
                var txt = await File.ReadAllTextAsync(filePath, cancellationToken);
                pages.Add(new PageContent { PageNumber = 1, Content = txt });
                break;

            case ".pdf":
                using (var document = PdfDocument.Open(filePath))
                {
                    foreach (var page in document.GetPages())
                    {
                        var words = page.GetWords().ToList();
                        if (words.Count == 0) continue;

                        var lineBuilder = new StringBuilder();
                        var lastWord = words[0];
                        lineBuilder.Append(lastWord.Text);

                        for (int i = 1; i < words.Count; i++)
                        {
                            var word = words[i];
                            var yDiff = Math.Abs(word.BoundingBox.Bottom - lastWord.BoundingBox.Bottom);

                            if (yDiff > 2)
                            {
                                lineBuilder.AppendLine();
                                if (yDiff > 10)
                                {
                                    lineBuilder.AppendLine();
                                }
                            }
                            else
                            {
                                lineBuilder.Append(" ");
                            }
                            lineBuilder.Append(word.Text);
                            lastWord = word;
                        }

                        pages.Add(new PageContent 
                        { 
                            PageNumber = page.Number, 
                            Content = lineBuilder.ToString() 
                        });
                    }
                }
                break;

            case ".docx":
                using (var doc = WordprocessingDocument.Open(filePath, false))
                {
                    var body = doc.MainDocumentPart?.Document.Body;
                    if (body != null)
                    {
                        var hasPageBreaks = body.Descendants<WBreak>().Any(b => b.Type != null && b.Type.Value == BreakValues.Page) ||
                                            body.Descendants<LastRenderedPageBreak>().Any();

                        if (hasPageBreaks)
                        {
                            int currentPage = 1;
                            var currentSb = new StringBuilder();

                            foreach (var paragraph in body.Descendants<Paragraph>())
                            {
                                var pText = string.Concat(paragraph.Descendants<WText>().Select(t => t.Text));
                                if (!string.IsNullOrWhiteSpace(pText))
                                {
                                    currentSb.AppendLine(pText);
                                    currentSb.AppendLine();
                                }

                                if (paragraph.Descendants<WBreak>().Any(b => b.Type != null && b.Type.Value == BreakValues.Page) ||
                                    paragraph.Descendants<LastRenderedPageBreak>().Any())
                                {
                                    if (currentSb.Length > 0)
                                    {
                                        pages.Add(new PageContent { PageNumber = currentPage, Content = currentSb.ToString() });
                                        currentSb.Clear();
                                    }
                                    currentPage++;
                                }
                            }

                            if (currentSb.Length > 0)
                            {
                                pages.Add(new PageContent { PageNumber = currentPage, Content = currentSb.ToString() });
                            }
                        }
                        else
                        {
                            int currentPage = 1;
                            var currentSb = new StringBuilder();
                            int wordCount = 0;

                            foreach (var paragraph in body.Descendants<Paragraph>())
                            {
                                var pText = string.Concat(paragraph.Descendants<WText>().Select(t => t.Text));
                                if (string.IsNullOrWhiteSpace(pText)) continue;

                                currentSb.AppendLine(pText);
                                currentSb.AppendLine();

                                var words = pText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                wordCount += words.Length;

                                if (wordCount >= 450)
                                {
                                    pages.Add(new PageContent { PageNumber = currentPage, Content = currentSb.ToString() });
                                    currentSb.Clear();
                                    wordCount = 0;
                                    currentPage++;
                                }
                            }

                            if (currentSb.Length > 0)
                            {
                                pages.Add(new PageContent { PageNumber = currentPage, Content = currentSb.ToString() });
                            }
                        }
                    }
                }
                break;

            case ".pptx":
                using (var doc = PresentationDocument.Open(filePath, false))
                {
                    var presentation = doc.PresentationPart?.Presentation;
                    if (presentation?.SlideIdList != null)
                    {
                        int slideIndex = 1;
                        foreach (var slideId in presentation.SlideIdList.Elements<SlideId>())
                        {
                            var slidePart = doc.PresentationPart?.GetPartById(slideId.RelationshipId!) as SlidePart;
                            if (slidePart?.Slide == null) continue;

                            var sb = new StringBuilder();
                            foreach (var textNode in slidePart.Slide.Descendants<A.Text>())
                            {
                                if (!string.IsNullOrWhiteSpace(textNode.Text))
                                {
                                    sb.AppendLine(textNode.Text);
                                }
                            }
                            pages.Add(new PageContent { PageNumber = slideIndex++, Content = sb.ToString() });
                        }
                    }
                }
                break;

            case ".doc":
                try
                {
                    var doc = new Spire.Doc.Document();
                    doc.LoadFromFile(filePath, FileFormat.Doc);
                    var fullText = doc.GetText();

                    if (!string.IsNullOrWhiteSpace(fullText))
                    {
                        int currentPage = 1;
                        var currentSb = new StringBuilder();
                        int wordCount = 0;

                        foreach (var line in fullText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            if (line.Contains("Evaluation Warning: The document was created with Spire.Doc for .NET.")) continue; // Loại bỏ watermark của bản Free

                            currentSb.AppendLine(line);
                            var words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            wordCount += words.Length;

                            if (wordCount >= 450)
                            {
                                pages.Add(new PageContent { PageNumber = currentPage, Content = currentSb.ToString() });
                                currentSb.Clear();
                                wordCount = 0;
                                currentPage++;
                            }
                        }

                        if (currentSb.Length > 0)
                        {
                            pages.Add(new PageContent { PageNumber = currentPage, Content = currentSb.ToString() });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi đọc file .doc bằng FreeSpire.Doc");
                    throw;
                }
                break;
        }

        return pages;
    }

    private string ExtractPdfText(string filePath)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;
            
            var lineBuilder = new StringBuilder();
            var lastWord = words[0];
            lineBuilder.Append(lastWord.Text);

            for (int i = 1; i < words.Count; i++)
            {
                var word = words[i];
                var yDiff = Math.Abs(word.BoundingBox.Bottom - lastWord.BoundingBox.Bottom);
                
                if (yDiff > 2) // Khác dòng
                {
                    lineBuilder.AppendLine();
                    if (yDiff > 10) // Khoảng cách lớn -> Đoạn văn mới
                    {
                        lineBuilder.AppendLine();
                    }
                }
                else
                {
                    lineBuilder.Append(" ");
                }
                lineBuilder.Append(word.Text);
                lastWord = word;
            }
            sb.AppendLine(lineBuilder.ToString());
            sb.AppendLine();
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
                sb.AppendLine();
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
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string ExtractDocText(string filePath)
    {
        var doc = new Spire.Doc.Document();
        doc.LoadFromFile(filePath, FileFormat.Doc);
        var text = doc.GetText();
        return text.Replace("Evaluation Warning: The document was created with Spire.Doc for .NET.", "").Trim();
    }
}
