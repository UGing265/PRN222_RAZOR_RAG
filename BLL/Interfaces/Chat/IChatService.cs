using BLL.DTOs.Chat;

namespace BLL.Interfaces.Chat;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(Guid userId, ChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamMessageAsync(Guid userId, ChatRequest request, CancellationToken cancellationToken = default);
    Task<List<ChatSessionSummaryDto>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<ChatMessageDto>> GetSessionMessagesAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionsAsync(Guid userId, List<Guid> sessionIds, CancellationToken cancellationToken = default);
}
