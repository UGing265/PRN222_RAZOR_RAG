using System.Security.Claims;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Controllers;

[Authorize]
[Route("dashboard")]
public class DashboardController : Controller
{
    private readonly IDocumentService _documentService;

    public DashboardController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var summary = await _documentService.GetDashboardSummaryAsync(userId, cancellationToken);
        ViewBag.TotalDocuments = summary.TotalDocuments;
        ViewBag.TotalChunks = summary.TotalChunks;
        ViewBag.TotalFiles = summary.TotalFiles;
        ViewBag.ApprovedDocuments = summary.ApprovedDocuments;
        ViewBag.PendingDocuments = summary.PendingDocuments;
        ViewBag.RejectedDocuments = summary.RejectedDocuments;
        ViewBag.RecentDocuments = summary.RecentDocuments;
        ViewBag.ActiveUploadJobs = summary.ActiveUploadJobs;

        if (!string.IsNullOrWhiteSpace(summary.CompletedUploadMessage))
        {
            TempData["SuccessMessage"] = summary.CompletedUploadMessage;
        }

        return View();
    }

    [HttpGet("upload-jobs")]
    public async Task<IActionResult> UploadJobsPartial(CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        ViewBag.ActiveUploadJobs = await _documentService.GetUploadJobsAsync(userId, cancellationToken);
        return PartialView("_UploadJobs");
    }
}
