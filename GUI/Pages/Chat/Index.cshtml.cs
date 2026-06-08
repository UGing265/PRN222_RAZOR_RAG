using BLL.DTOs.Chat;
using BLL.Interfaces.Chat;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace GUI.Pages.Chat;

[Authorize(Roles = "Lecturer,Student")]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly IChatService _chatService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IChatService chatService, IDocumentService documentService, ILogger<IndexModel> logger)
    {
        _chatService = chatService;
        _documentService = documentService;
        _logger = logger;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetSessionsAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var sessions = await _chatService.GetSessionsAsync(userId.Value, cancellationToken);
        return new JsonResult(sessions);
    }

    public async Task<IActionResult> OnGetSessionMessagesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var messages = await _chatService.GetSessionMessagesAsync(userId.Value, sessionId, cancellationToken);
        return new JsonResult(messages);
    }

    public async Task<IActionResult> OnGetDocumentsAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _documentService.GetAllDocumentsAsync(
            query: null,
            subjectId: null,
            page: 1,
            pageSize: 100,
            requesterUserId: userId.Value,
            cancellationToken: cancellationToken);

        var documents = result.Documents
            .Where(d => d.Status == "approved" || d.Status == "done")
            .Select(d => new { id = d.Id, title = d.Title, subject = d.SubjectName, status = d.Status })
            .ToList();

        return new JsonResult(documents);
    }

    public async Task<IActionResult> OnPostSendMessageAsync([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        var response = await _chatService.SendMessageAsync(userId.Value, request, cancellationToken);
        return new JsonResult(response);
    }

    public async Task OnPostStreamAsync([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            Response.StatusCode = 401;
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = 400;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            await foreach (var chunk in _chatService.StreamMessageAsync(userId.Value, request, cancellationToken))
            {
                await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Stream cancelled by client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stream failed.");
            await Response.WriteAsync($"data: [ERROR] Đã xảy ra lỗi khi xử lý tin nhắn.\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }
}
