using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;
using Markdig;

namespace GUI.Pages.Documents;

[Authorize]
public class CompareModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentComparisonService _documentComparisonService;

    public CompareModel(IDocumentService documentService, IDocumentComparisonService documentComparisonService)
    {
        _documentService = documentService;
        _documentComparisonService = documentComparisonService;
    }

    public string? ComparisonResultHtml { get; set; }
    public string? ErrorMessage { get; set; }
    
    // We need to keep track of the selected documents to re-render them if a postback happens.
    [BindProperty]
    public List<Guid> SelectedDocumentIds { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetSearchAsync(string query)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        // Search both owned and public documents
        var searchResult = await _documentService.GetAllDocumentsAsync(
            query: query,
            subjectId: null,
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
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }
}
