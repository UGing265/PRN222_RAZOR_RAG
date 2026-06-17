using System.Text.RegularExpressions;
using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BLL.Services.Documents;

public sealed class QuestPdfComparisonExporter : IComparisonPdfExporter
{
    private const string BrandName = "PRN222 RAG";
    private const string BrandTagline = "So sánh tài liệu bằng AI";

    private static readonly Color BrandColor = Color.FromHex("#0F172A");
    private static readonly Color MutedColor = Color.FromHex("#64748B");
    private static readonly Color AccentColor = Color.FromHex("#2563EB");

    public byte[] Build(ComparisonExportRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.RawMarkdown))
            throw new ArgumentException("RawMarkdown is required.", nameof(request));

        var blocks = MarkdownBlockParser.Parse(request.RawMarkdown);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Segoe UI"));

                page.Header().Element(c => ComposeHeader(c, request));
                page.Content().Element(c => ComposeContent(c, blocks));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(9).FontColor(MutedColor));
                    t.Span(BrandName + " • ");
                    t.CurrentPageNumber().FontColor(MutedColor);
                    t.Span(" / ");
                    t.TotalPages().FontColor(MutedColor);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ComparisonExportRequest request)
    {
        container.PaddingBottom(12).Column(col =>
        {
            col.Item().Text(BrandName).FontSize(18).Bold().FontColor(BrandColor);
            col.Item().Text(BrandTagline).FontSize(10).FontColor(MutedColor);
            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(AccentColor);

            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Người xuất").FontSize(9).FontColor(MutedColor);
                    c.Item().Text(request.RequesterEmail).FontSize(11).SemiBold();
                });
                row.ConstantItem(160).AlignRight().Column(c =>
                {
                    c.Item().Text("Thời điểm (UTC)").FontSize(9).FontColor(MutedColor).AlignRight();
                    c.Item().Text(request.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"))
                        .FontSize(11).SemiBold().AlignRight();
                });
            });

            col.Item().PaddingTop(10).Text("Tài liệu so sánh").FontSize(10).FontColor(MutedColor);
            col.Item().Text(string.Join("  •  ", request.DocumentTitles))
                .FontSize(11).FontColor(BrandColor);

            col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void ComposeContent(IContainer container, IReadOnlyList<MarkdownBlock> blocks)
    {
        container.PaddingVertical(8).Column(col =>
        {
            col.Spacing(6);
            foreach (var block in blocks)
            {
                RenderBlock(col.Item(), block);
            }
        });
    }

    private static void RenderBlock(IContainer container, MarkdownBlock block)
    {
        switch (block.Kind)
        {
            case MarkdownBlockKind.Heading1:
                container.PaddingTop(8).Text(block.Text).FontSize(18).Bold().FontColor(BrandColor);
                break;
            case MarkdownBlockKind.Heading2:
                container.PaddingTop(6).Text(block.Text).FontSize(15).Bold().FontColor(BrandColor);
                break;
            case MarkdownBlockKind.Heading3:
                container.PaddingTop(4).Text(block.Text).FontSize(13).Bold();
                break;
            case MarkdownBlockKind.Paragraph:
                container.Text(block.Text).FontSize(11).LineHeight(1.35f);
                break;
            case MarkdownBlockKind.Bullet:
                container.Row(r =>
                {
                    r.ConstantItem(14).Text("•").FontSize(11);
                    r.RelativeItem().Text(block.Text).FontSize(11).LineHeight(1.35f);
                });
                break;
            case MarkdownBlockKind.Code:
                container.Background(Colors.Grey.Lighten4).Padding(8)
                    .Text(block.Text).FontFamily("Consolas").FontSize(10);
                break;
            case MarkdownBlockKind.Table:
                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        var firstRow = block.TableRows?.FirstOrDefault();
                        var colCount = firstRow?.Count ?? 1;
                        for (var i = 0; i < colCount; i++) columns.RelativeColumn();
                    });

                    var isHeader = true;
                    foreach (var row in block.TableRows ?? Array.Empty<IReadOnlyList<string>>())
                    {
                        if (isHeader)
                        {
                            table.Header(h =>
                            {
                                foreach (var cell in row)
                                {
                                    h.Cell().Background(AccentColor).Padding(4)
                                        .Text(cell).FontColor(Colors.White).Bold().FontSize(10);
                                }
                            });
                            isHeader = false;
                        }
                        else
                        {
                            foreach (var cell in row)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(4).Text(cell).FontSize(10);
                            }
                        }
                    }
                });
                break;
        }
    }
}

internal enum MarkdownBlockKind
{
    Paragraph,
    Heading1,
    Heading2,
    Heading3,
    Bullet,
    Code,
    Table,
}

internal sealed record MarkdownBlock(
    MarkdownBlockKind Kind,
    string Text,
    IReadOnlyList<IReadOnlyList<string>>? TableRows = null);

internal static class MarkdownBlockParser
{
    private static readonly Regex TableSeparator = new(@"^\s*\|?\s*[:\-\| ]+\s*\|?\s*$", RegexOptions.Compiled);

    public static IReadOnlyList<MarkdownBlock> Parse(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var blocks = new List<MarkdownBlock>();
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            // Skip blank lines
            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

            // Code fence
            if (line.TrimStart().StartsWith("```"))
            {
                var sb = new System.Text.StringBuilder();
                i++;
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                {
                    sb.AppendLine(lines[i]);
                    i++;
                }
                if (i < lines.Length) i++; // skip closing fence
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Code, sb.ToString().TrimEnd()));
                continue;
            }

            // Table
            if (line.Contains('|') && i + 1 < lines.Length && TableSeparator.IsMatch(lines[i + 1]))
            {
                var headerCells = SplitTableRow(line);
                i += 2; // skip header + separator
                var rows = new List<IReadOnlyList<string>> { headerCells };
                while (i < lines.Length && lines[i].Contains('|') && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    rows.Add(SplitTableRow(lines[i]));
                    i++;
                }
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Table, string.Empty, rows));
                continue;
            }

            // Headings
            if (line.StartsWith("### "))
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Heading3, line[4..].Trim()));
                i++; continue;
            }
            if (line.StartsWith("## "))
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Heading2, line[3..].Trim()));
                i++; continue;
            }
            if (line.StartsWith("# "))
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Heading1, line[2..].Trim()));
                i++; continue;
            }

            // Bullet
            var bulletMatch = Regex.Match(line, @"^\s*[-*+]\s+(.*)$");
            if (bulletMatch.Success)
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Bullet, bulletMatch.Groups[1].Value.Trim()));
                i++; continue;
            }

            // Paragraph: consume consecutive non-empty, non-special lines
            var para = new System.Text.StringBuilder(line.Trim());
            i++;
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])
                && !lines[i].StartsWith("#") && !Regex.IsMatch(lines[i], @"^\s*[-*+]\s+")
                && !(lines[i].Contains('|') && i + 1 < lines.Length && TableSeparator.IsMatch(lines[i + 1])))
            {
                para.Append(' ').Append(lines[i].Trim());
                i++;
            }
            blocks.Add(new MarkdownBlock(MarkdownBlockKind.Paragraph, para.ToString()));
        }

        return blocks;
    }

    private static IReadOnlyList<string> SplitTableRow(string line)
    {
        var trimmed = line.Trim().Trim('|');
        return trimmed.Split('|').Select(c => c.Trim()).ToArray();
    }
}
