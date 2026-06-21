using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text.Json;
using Markdig;

namespace GUI.Pages.Documents;

[Authorize]
public class CompareModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentComparisonService _documentComparisonService;
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

    public string? ComparisonResultHtml { get; set; }
    public string? ErrorMessage { get; set; }
    
    // We need to keep track of the selected documents to re-render them if a postback happens.
    [BindProperty]
    public List<Guid> SelectedDocumentIds { get; set; } = new();

    public List<SubjectDto> Subjects { get; set; } = new();

    public string? ExportKey { get; set; }

    public async Task OnGetAsync()
    {
        Subjects = await _documentService.GetSubjectsAsync();
    }

    public async Task<IActionResult> OnGetSearchAsync(string? query, Guid? subjectId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        // Search both owned and public documents
        var searchResult = await _documentService.GetAllDocumentsAsync(
            query: query,
            subjectId: subjectId,
            page: 1,
            pageSize: 10,
            requesterUserId: userId,
            sortBy: "date_desc"
        );

        var documents = searchResult.Documents.Select(d => new
        {
            id = d.Id,
            title = d.Title,
            subjectName = d.SubjectName ?? "Không có môn học",
            visibility = d.Visibility,
            ownerEmail = d.OwnerEmail
        });

        return new JsonResult(documents);
    }

    public async Task<IActionResult> OnGetExportPdfAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return BadRequest();

        if (!_cache.TryGetValue<ComparisonExportRequest>(key, out var payload) || payload is null)
        {
            ErrorMessage = "Phiên xuất PDF đã hết hạn. Vui lòng chạy lại phân tích.";
            return Page();
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

    public async Task<IActionResult> OnPostAsync()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        if (SelectedDocumentIds == null || SelectedDocumentIds.Count < 2 || SelectedDocumentIds.Count > 5)
        {
            ErrorMessage = "Vui lòng chọn từ 2 đến 5 tài liệu để so sánh.";
            Subjects = await _documentService.GetSubjectsAsync();
            return Page();
        }

        var isAdmin = User.IsInRole("Admin");

        try
        {
            var rawMarkdown = await _documentComparisonService.CompareDocumentsAsync(SelectedDocumentIds, userId, isAdmin);
            
            // Log the raw markdown to see what Gemini actually returned
            Console.WriteLine("=== RAW MARKDOWN FROM GEMINI ===");
            Console.WriteLine(rawMarkdown);
            Console.WriteLine("================================");
            
            var pipeline = new Markdig.MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UsePipeTables()
                .Build();
            ComparisonResultHtml = Markdig.Markdown.ToHtml(rawMarkdown, pipeline);

            // Stash raw markdown + metadata in cache so the export handler can build a PDF
            var exportKey = Guid.NewGuid().ToString("N");
            var titles = await ResolveDocumentTitlesAsync(SelectedDocumentIds);
            var cacheEntry = new ComparisonExportRequest
            {
                RawMarkdown = rawMarkdown,
                DocumentTitles = titles,
                RequesterEmail = User.FindFirstValue(ClaimTypes.Email) ?? userIdString,
                GeneratedAtUtc = DateTime.UtcNow,
            };
            _cache.Set(exportKey, cacheEntry, TimeSpan.FromMinutes(5));
            ExportKey = exportKey;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        Subjects = await _documentService.GetSubjectsAsync();
        return Page();
    }

    private async Task<IReadOnlyList<string>> ResolveDocumentTitlesAsync(List<Guid> ids)
    {
        var titles = new List<string>(ids.Count);
        foreach (var id in ids)
        {
            var doc = await _documentService.GetDocumentDetailsAsync(id, chunkPage: 1, chunkPageSize: 1, incrementViewCount: false);
            titles.Add(doc?.Title ?? $"Tài liệu {id.ToString().Substring(0, 8)}");
        }
        return titles;
    }
}
