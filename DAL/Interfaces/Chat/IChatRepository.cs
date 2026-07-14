using DAL.Entities;
using Pgvector;

namespace DAL.Interfaces.Chat;

public interface IChatRepository
{
    Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<List<ChatSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<List<ChatMessage>> GetRecentMessagesAsync(Guid sessionId, int count, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> SearchSimilarChunksAsync(Vector queryEmbedding, int topK, List<Guid>? documentIds, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> GetChunksByIdsAsync(List<Guid> chunkIds, CancellationToken cancellationToken = default);
    Task UpdateSessionTitleAsync(Guid sessionId, string title, CancellationToken cancellationToken = default);
    Task AddDocumentToSessionAsync(Guid sessionId, Guid documentId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetActiveDocumentIdsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task DeleteSessionsAsync(List<Guid> sessionIds, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
