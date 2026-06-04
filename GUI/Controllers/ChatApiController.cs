using System.Security.Claims;
using BLL.DTOs.Chat;
using BLL.Interfaces.Chat;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Controllers;

[Route("api/chat")]
[ApiController]
[Authorize(Roles = "Lecturer,Student")]
public class ChatApiController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<ChatApiController> _logger;

    public ChatApiController(IChatService chatService, IDocumentService documentService, ILogger<ChatApiController> logger)
    {
        _chatService = chatService;
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Gửi tin nhắn và nhận response đầy đủ (JSON)
    /// POST /api/chat/message
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message is required." });

            var response = await _chatService.SendMessageAsync(userId.Value, request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessage failed.");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi xử lý tin nhắn. Vui lòng thử lại." });
        }
    }

    /// <summary>
    /// Gửi tin nhắn và nhận stream response (Server-Sent Events)
    /// POST /api/chat/stream
    /// </summary>
    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequest request, CancellationToken cancellationToken)
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

            // Signal end of stream
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, đây là bình thường
            _logger.LogDebug("Stream cancelled by client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stream failed.");
            await Response.WriteAsync($"data: [ERROR] Đã xảy ra lỗi khi xử lý tin nhắn.\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Lấy danh sách phiên chat của user
    /// GET /api/chat/sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var sessions = await _chatService.GetSessionsAsync(userId.Value, cancellationToken);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSessions failed.");
            return StatusCode(500, new { error = "Không thể tải danh sách phiên chat." });
        }
    }

    /// <summary>
    /// Lấy lịch sử tin nhắn của một phiên
    /// GET /api/chat/sessions/{sessionId}/messages
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/messages")]
    public async Task<IActionResult> GetSessionMessages(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var messages = await _chatService.GetSessionMessagesAsync(userId.Value, sessionId, cancellationToken);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSessionMessages failed. SessionId={SessionId}", sessionId);
            return StatusCode(500, new { error = "Không thể tải lịch sử tin nhắn." });
        }
    }

    /// <summary>
    /// Lấy danh sách tài liệu khả dụng để chat
    /// GET /api/chat/documents
    /// </summary>
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
    {
        try
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

            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDocuments failed.");
            return StatusCode(500, new { error = "Không thể tải danh sách tài liệu." });
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
