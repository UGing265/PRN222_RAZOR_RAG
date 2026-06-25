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

    public List<DocumentListItemDto> CompareDocuments { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Subjects = await _documentService.GetSubjectsAsync();
        
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idClaim, out var userId))
        {
            var result = await _documentService.GetAllDocumentsAsync(
                query: null,
                subjectId: null,
                page: 1,
                pageSize: 100,
                requesterUserId: userId,
                cancellationToken: cancellationToken);

            CompareDocuments = result.Documents
                .Where(d => d.Status == "approved" || d.Status == "done" || d.Status == "completed")
                .ToList();
        }
    }


    public async Task<IActionResult> OnGetExportPdfAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return BadRequest();

        if (!_cache.TryGetValue<ComparisonExportRequest>(key, out var payload) || payload is null)
        {
            TempData["ErrorMessage"] = "Phiên xuất PDF đã hết hạn. Vui lòng chạy lại phân tích.";
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

    public List<DocumentDetailsDto> SelectedDocuments { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        if (SelectedDocumentIds == null || SelectedDocumentIds.Count < 2 || SelectedDocumentIds.Count > 5)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn từ 2 đến 5 tài liệu để so sánh.";
            Subjects = await _documentService.GetSubjectsAsync();
            await PopulateSelectedDocumentsAsync();
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
            TempData["ErrorMessage"] = ex.Message;
        }

        Subjects = await _documentService.GetSubjectsAsync();
        await PopulateSelectedDocumentsAsync();
        return Page();
    }

    private async Task PopulateSelectedDocumentsAsync()
    {
        if (SelectedDocumentIds != null && SelectedDocumentIds.Any())
        {
            foreach (var id in SelectedDocumentIds)
            {
                var doc = await _documentService.GetDocumentDetailsAsync(id, 1, 1, false);
                if (doc != null)
                {
                    SelectedDocuments.Add(doc);
                }
            }
        }
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
