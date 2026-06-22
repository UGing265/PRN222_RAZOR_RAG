# Compare PDF Export Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm nút "Xuất PDF" cạnh kết quả so sánh tài liệu tại `/Documents/Compare`, cho phép người dùng tải kết quả phân tích AI dưới dạng file PDF có branding + metadata (ngày tạo, email user, tên tài liệu so sánh).

**Architecture:**
- Backend: Service `IComparisonPdfExporter` (BLL) dùng QuestPDF render markdown thô (trả về từ `IDocumentComparisonService`) thành PDF. PDF gồm header (logo text + ngày + user email), danh sách tài liệu so sánh, nội dung markdown (heading/list/table/paragraph).
- Glue: Sau khi `OnPostAsync` ở `Compare.cshtml.cs` generate comparison thành công, lưu kết quả (markdown + metadata) vào `IMemoryCache` với key ngẫu nhiên + TTL 5 phút. UI render nút `<a href="?handler=ExportPdf&key={key}">Xuất PDF</a>`.
- Handler mới: `OnGetExportPdfAsync(string key)` lấy data từ cache, gọi exporter, trả về `FileContentResult` với filename `compare-{yyyyMMdd-HHmmss}.pdf`.

**Tech Stack:**
- QuestPDF 2024.10.x (Community License) — server-side PDF generation
- Markdig (đã có) — đã dùng ở trang Compare
- `IMemoryCache` (đã đăng ký trong `Program.cs:14`)

---

## File Structure

**New files:**
- `BLL/DTOs/Documents/ComparisonExportRequest.cs` — DTO input cho exporter (raw markdown + list document titles + user email)
- `BLL/Interfaces/Documents/IComparisonPdfExporter.cs` — service interface
- `BLL/Services/Documents/QuestPdfComparisonExporter.cs` — QuestPDF implementation

**Modified files:**
- `BLL/BLL.csproj` — thêm PackageReference `QuestPDF`
- `BLL/Extensions/ServiceCollectionExtensions.cs` — đăng ký `IComparisonPdfExporter` trong DI
- `GUI/Program.cs` — set QuestPDF Community License 1 lần ở startup
- `GUI/Pages/Documents/Compare.cshtml.cs` — lưu comparison vào cache sau POST, thêm `OnGetExportPdfAsync` handler
- `GUI/Pages/Documents/Compare.cshtml` — thêm nút "Xuất PDF" cạnh tiêu đề kết quả

---

## Task 1: Thêm QuestPDF package + set Community License

**Files:**
- Modify: `BLL/BLL.csproj`
- Modify: `GUI/Program.cs`

- [ ] **Step 1: Thêm PackageReference QuestPDF vào BLL.csproj**

Thêm vào trong `<ItemGroup>` package references (cùng nhóm với `UglyToad.PdfPig`):

```xml
<PackageReference Include="QuestPDF" Version="2024.10.0" />
```

- [ ] **Step 2: Restore package**

Run:
```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG/BLL" && dotnet restore
```
Expected: Không lỗi, package QuestPDF 2024.10.0 được thêm.

- [ ] **Step 3: Set Community License trong GUI/Program.cs**

Thêm using ở đầu file (cùng nhóm với các using `BLL.*`):
```csharp
using QuestPDF.Infrastructure;
```

Ngay sau `var builder = WebApplication.CreateBuilder(args);` (dòng 10), thêm:
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```

- [ ] **Step 4: Build để kiểm tra compile**

Run:
```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG/GUI" && dotnet build
```
Expected: Build succeeded. Có thể có warning về QuestPDF license (Community) — OK.

- [ ] **Step 5: Commit**

```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG" && git add BLL/BLL.csproj GUI/Program.cs && git commit -m "feat(compare): add QuestPDF package and set community license"
```

---

## Task 2: Tạo DTO + Interface cho exporter

**Files:**
- Create: `BLL/DTOs/Documents/ComparisonExportRequest.cs`
- Create: `BLL/Interfaces/Documents/IComparisonPdfExporter.cs`

- [ ] **Step 1: Tạo DTO**

Tạo file `BLL/DTOs/Documents/ComparisonExportRequest.cs`:

```csharp
namespace BLL.DTOs.Documents;

/// <summary>
/// Input payload for exporting a document-comparison result to PDF.
/// </summary>
public sealed class ComparisonExportRequest
{
    /// <summary>Raw markdown returned by the comparison service.</summary>
    public required string RawMarkdown { get; init; }

    /// <summary>Titles of the documents that were compared (2-5 entries).</summary>
    public required IReadOnlyList<string> DocumentTitles { get; init; }

    /// <summary>Email of the user requesting the export (for the header).</summary>
    public required string RequesterEmail { get; init; }

    /// <summary>UTC timestamp when the comparison was generated.</summary>
    public required DateTime GeneratedAtUtc { get; init; }
}
```

- [ ] **Step 2: Tạo interface**

Tạo file `BLL/Interfaces/Documents/IComparisonPdfExporter.cs`:

```csharp
namespace BLL.Interfaces.Documents;

/// <summary>
/// Renders a document-comparison result (markdown) into a downloadable PDF.
/// </summary>
public interface IComparisonPdfExporter
{
    /// <summary>
    /// Build a PDF byte array from the given comparison request.
    /// </summary>
    /// <returns>PDF file content (bytes).</returns>
    byte[] Build(ComparisonExportRequest request);
}
```

- [ ] **Step 3: Build để kiểm tra compile**

Run:
```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG/BLL" && dotnet build
```
Expected: Build succeeded (DTO + interface chỉ là contract, chưa cần impl).

- [ ] **Step 4: Commit**

```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG" && git add BLL/DTOs/Documents/ComparisonExportRequest.cs BLL/Interfaces/Documents/IComparisonPdfExporter.cs && git commit -m "feat(compare): add IComparisonPdfExporter contract"
```

---

## Task 3: Implement QuestPdfComparisonExporter

**Files:**
- Create: `BLL/Services/Documents/QuestPdfComparisonExporter.cs`
- Modify: `BLL/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Tạo class exporter**

Tạo file `BLL/Services/Documents/QuestPdfComparisonExporter.cs`:

```csharp
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
                container.Text(block.Text).FontSize(18).Bold().FontColor(BrandColor).PaddingTop(8);
                break;
            case MarkdownBlockKind.Heading2:
                container.Text(block.Text).FontSize(15).Bold().FontColor(BrandColor).PaddingTop(6);
                break;
            case MarkdownBlockKind.Heading3:
                container.Text(block.Text).FontSize(13).Bold().PaddingTop(4);
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
                        var firstRow = block.TableRows.FirstOrDefault();
                        var colCount = firstRow?.Count ?? 1;
                        for (var i = 0; i < colCount; i++) columns.RelativeColumn();
                    });

                    var isHeader = true;
                    foreach (var row in block.TableRows)
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
```

- [ ] **Step 2: Đăng ký service trong DI**

Mở `BLL/Extensions/ServiceCollectionExtensions.cs`. Trong method `AddBusinessLayer`, ngay sau dòng đăng ký `IDocumentComparisonService` (dòng 43), thêm:

```csharp
services.AddSingleton<IComparisonPdfExporter, QuestPdfComparisonExporter>();
```

(Đăng ký Singleton vì exporter stateless, QuestPDF document builder là thread-safe theo docs.)

- [ ] **Step 3: Build để kiểm tra compile**

Run:
```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG/BLL" && dotnet build
```
Expected: Build succeeded. Cảnh báo QuestPDF community license là bình thường.

- [ ] **Step 4: Commit**

```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG" && git add BLL/Services/Documents/QuestPdfComparisonExporter.cs BLL/Extensions/ServiceCollectionExtensions.cs && git commit -m "feat(compare): implement QuestPDF-based comparison exporter"
```

---

## Task 4: Cache + Export handler trong Compare page

**Files:**
- Modify: `GUI/Pages/Documents/Compare.cshtml.cs`

- [ ] **Step 1: Thêm dependency + cache key property**

Mở `GUI/Pages/Documents/Compare.cshtml.cs`. Thêm using ở đầu (cùng nhóm với các using khác):

```csharp
using BLL.DTOs.Documents;
using Microsoft.Extensions.Caching.Memory;
```

Inject thêm vào constructor (sau `_documentComparisonService`):

```csharp
private readonly IComparisonPdfExporter _pdfExporter;
private readonly IMemoryCache _cache;

public CompareModel(
    IDocumentService documentService,
    IDocumentComparisonService documentComparisonService,
    IComparisonPdfExporter pdfExporter,
    IMemoryCache cache)
{
    _documentService = documentService;
    _documentComparisonService = documentComparisonService;
    _pdfExporter = pdfExporter;
    _cache = cache;
}
```

Thêm property ngay sau `SelectedDocumentIds`:

```csharp
public string? ExportKey { get; set; }
```

- [ ] **Step 2: Lưu kết quả vào cache sau khi compare thành công**

Trong method `OnPostAsync`, ngay sau khi `ComparisonResultHtml` được gán (sau dòng `ComparisonResultHtml = Markdig.Markdown.ToHtml(rawMarkdown, pipeline);`), thêm khối code để lưu vào cache và lấy `ExportKey`:

```csharp
// Stash raw markdown + metadata in cache so the export handler can build a PDF
var exportKey = Guid.NewGuid().ToString("N");
var titles = await ResolveDocumentTitlesAsync(SelectedDocumentIds, userId, isAdmin);
var cacheEntry = new ComparisonExportRequest
{
    RawMarkdown = rawMarkdown,
    DocumentTitles = titles,
    RequesterEmail = User.FindFirstValue(ClaimTypes.Email) ?? userIdString,
    GeneratedAtUtc = DateTime.UtcNow,
};
_cache.Set(exportKey, cacheEntry, TimeSpan.FromMinutes(5));
ExportKey = exportKey;
```

- [ ] **Step 3: Thêm helper `ResolveDocumentTitlesAsync`**

Thêm method private ngay trước dấu đóng `}` cuối class:

```csharp
private async Task<IReadOnlyList<string>> ResolveDocumentTitlesAsync(
    List<Guid> ids, Guid userId, bool isAdmin)
{
    var titles = new List<string>(ids.Count);
    foreach (var id in ids)
    {
        var doc = await _documentService.GetDocumentDetailAsync(id, userId, isAdmin);
        titles.Add(doc?.Title ?? $"Tài liệu {id.ToString().Substring(0, 8)}");
    }
    return titles;
}
```

- [ ] **Step 4: Thêm handler `OnGetExportPdfAsync`**

Thêm method ngay sau `OnGetSearchAsync`:

```csharp
public async Task<IActionResult> OnGetExportPdfAsync(string key)
{
    if (string.IsNullOrWhiteSpace(key)) return BadRequest();

    if (!_cache.TryGetValue<ComparisonExportRequest>(key, out var payload) || payload is null)
    {
        ErrorMessage = "Phiên xuất PDF đã hết hạn. Vui lòng chạy lại phân tích.";
        return Page();
    }

    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdString, out var userId))
    {
        return Unauthorized();
    }

    // Ownership check: only the original requester (or admin) can download.
    var requesterEmail = User.FindFirstValue(ClaimTypes.Email);
    if (!User.IsInRole("Admin") &&
        !string.Equals(requesterEmail, payload.RequesterEmail, StringComparison.OrdinalIgnoreCase))
    {
        return Forbid();
    }

    var pdfBytes = _pdfExporter.Build(payload);
    var fileName = $"compare-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
    return File(pdfBytes, "application/pdf", fileName);
}
```

- [ ] **Step 5: Build để kiểm tra compile**

Run:
```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG/GUI" && dotnet build
```
Expected: Build succeeded. Cần đảm bảo `IDocumentService` có method `GetDocumentDetailAsync` — nếu tên khác, điều chỉnh theo signature thực tế trong `BLL/Interfaces/Documents/IDocumentService.cs`.

- [ ] **Step 6: Commit**

```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG" && git add GUI/Pages/Documents/Compare.cshtml.cs && git commit -m "feat(compare): cache comparison result and add export handler"
```

---

## Task 5: Thêm nút "Xuất PDF" trên UI

**Files:**
- Modify: `GUI/Pages/Documents/Compare.cshtml`

- [ ] **Step 1: Sửa tiêu đề kết quả thành flex row với nút**

Trong `Compare.cshtml`, tìm khối:

```cshtml
<h3 class="font-serif tracking-tight text-text-primary mb-4">Kết Quả Phân Tích</h3>
```

Thay bằng:

```cshtml
<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <h3 class="font-serif tracking-tight text-text-primary mb-0">Kết Quả Phân Tích</h3>
    @if (!string.IsNullOrEmpty(Model.ExportKey))
    {
        <a href="?handler=ExportPdf&amp;key=@Model.ExportKey"
           class="btn btn-outline-primary btn-sm"
           download>
            <i class="bi bi-file-earmark-pdf me-1"></i> Xuất PDF
        </a>
    }
</div>
```

- [ ] **Step 2: Build + chạy app để kiểm tra thủ công**

Run:
```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG/GUI" && dotnet build
```
Expected: Build succeeded.

Mở browser `https://localhost:7065/Documents/Compare`, chọn 2-5 tài liệu, bấm "Phân Tích Sự Khác Biệt", kiểm tra:
- Kết quả render đúng như cũ
- Nút "Xuất PDF" xuất hiện cạnh tiêu đề "Kết Quả Phân Tích"
- Click nút → tải file PDF, mở được, nội dung khớp với kết quả markdown
- Refresh trang (mất POST state) → nút biến mất, không crash

- [ ] **Step 3: Commit**

```bash
cd "D:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG" && git add GUI/Pages/Documents/Compare.cshtml && git commit -m "feat(compare): add Xuất PDF button next to comparison result"
```

---

## Self-Review Checklist

- [x] **Spec coverage:** Header (branding + email + UTC time) ✓, document titles list ✓, markdown body (heading/list/table/paragraph) ✓, filename `compare-{yyyyMMdd-HHmmss}.pdf` ✓, trigger button next to result title ✓, server-side QuestPDF ✓.
- [x] **No placeholders:** Tất cả code blocks đầy đủ, không "TBD"/"TODO".
- [x] **Type consistency:** `IComparisonPdfExporter.Build(ComparisonExportRequest)` được dùng nhất quán giữa Task 2, 3, 4. `IMemoryCache` inject đúng kiểu. `ExportKey` được set trong POST và đọc trong Razor.
- [x] **Scope:** 1 feature duy nhất (PDF export), 1 plan, không cần tách.

**Ghi chú kỹ thuật:**
- Nếu `IDocumentService.GetDocumentDetailAsync` không tồn tại hoặc khác signature, tra cứu trong `BLL/Interfaces/Documents/IDocumentService.cs` và điều chỉnh `ResolveDocumentTitlesAsync` (ví dụ: dùng method trả về `DocumentDetailDto?` có property `Title`).
- Markdown parser chỉ hỗ trợ heading, bullet list, code fence, table, paragraph. Bold/italic inline sẽ hiển thị nguyên văn dấu `**`/`*` — chấp nhận được cho MVP. Nếu cần loại bỏ, mở rộng `MarkdownBlockParser` hoặc dùng Markdig AST.
