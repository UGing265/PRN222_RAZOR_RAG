using BLL.Interfaces.Documents;
using BLL.DTOs.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GUI.Pages.Chat;

[Authorize(Roles = "Lecturer,Student")]
public class IndexModel : PageModel
{
    private readonly IDocumentService _documentService;

    public IndexModel(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public List<DocumentListItemDto> ChatDocuments { get; set; } = new();
    public List<SubjectDto> AllSubjects { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
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

            ChatDocuments = result.Documents
                .Where(d => d.Status == "approved" || d.Status == "done" || d.Status == "completed")
                .ToList();
        }

        AllSubjects = await _documentService.GetSubjectsAsync(cancellationToken);
    }
}
