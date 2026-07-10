using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using BLL.DTOs.Chat;
using BLL.Interfaces.Chat;
using BLL.Interfaces.Documents;
using BLL.Interfaces.Tokens;
using DAL.Entities;
using DAL.Interfaces.Chat;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Chat;

public class ChatService : IChatService
{
    private const int MaxHistoryMessages = 10;
    private const int TopKChunks = 5;

    private readonly IChatRepository _chatRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IGeminiChatService _geminiChatService;
    private readonly ILogger<ChatService> _logger;
    private readonly ITokenUsageService _tokenUsageService;

    public ChatService(
        IChatRepository chatRepository,
        IEmbeddingService embeddingService,
        IGeminiChatService geminiChatService,
        ILogger<ChatService> logger,
        ITokenUsageService tokenUsageService)
    {
        _chatRepository = chatRepository;
        _embeddingService = embeddingService;
        _geminiChatService = geminiChatService;
        _logger = logger;
        _tokenUsageService = tokenUsageService;
    }

    /// <summary>
    /// Main flow: receive message -> embed -> search vector -> build prompt -> call LLM -> save to DB -> return result.
    /// </summary>
    public async Task<ChatResponse> SendMessageAsync(Guid userId, ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Tạo hoặc lấy Session
            var session = await GetOrCreateSessionAsync(userId, request, cancellationToken);

            // 2. Lưu tin nhắn User vào DB
            await SaveUserMessageAsync(userId, session.Id, request.Message, cancellationToken);

            // 3. Lấy lịch sử hội thoại gần nhất
            var historyMessages = await _chatRepository.GetRecentMessagesAsync(session.Id, MaxHistoryMessages, cancellationToken);

            // 4. Embed câu hỏi thành vector
            _logger.LogInformation("Embedding user question for session {SessionId}", session.Id);
            var enhancedQuery = await EnhanceQueryAsync(request.Message, cancellationToken);
            var queryEmbedding = await _embeddingService.EmbedAsync(enhancedQuery, cancellationToken);

            // 5. Tìm Top K chunks liên quan
            _logger.LogInformation("Searching similar chunks. DocumentIds={DocumentIds}, TopK={TopK}", string.Join(",", request.DocumentIds ?? new List<Guid>()), TopKChunks);
            var relevantChunks = await _chatRepository.SearchSimilarChunksAsync(queryEmbedding, TopKChunks, request.DocumentIds, cancellationToken);

            // 6. Build System Prompt
            var systemPrompt = BuildSystemPrompt(relevantChunks);

            // 7. Build conversation history cho Gemini
            var geminiHistory = BuildGeminiHistory(historyMessages);

            // 8. Gọi Gemini để sinh câu trả lời
            _logger.LogInformation("Calling Gemini for chat response. SessionId={SessionId}, ChunksFound={ChunksFound}", session.Id, relevantChunks.Count);
            var reply = await _geminiChatService.GenerateAsync(systemPrompt, geminiHistory, cancellationToken);

            // 9. Lưu response Assistant vào DB (kèm RetrievedChunkIds để phục vụ "Nguồn trích dẫn" khi load lại lịch sử)
            var chunkIds = relevantChunks.Select(c => c.Id).ToList();
            await SaveAssistantMessageAsync(userId, session.Id, reply, chunkIds, cancellationToken);

            // 10. Extract sources và trả về
            var sources = ExtractSources(relevantChunks);

            return new ChatResponse
            {
                SessionId = session.Id,
                SessionTitle = session.Title,
                Reply = reply,
                Sources = sources
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessageAsync failed. UserId={UserId}, SessionId={SessionId}", userId, request.SessionId);
            throw;
        }
    }

    /// <summary>
    /// Streaming flow: similar to SendMessageAsync but yields text chunks for real-time UI display.
    /// Note: the full response will be saved to the DB after the stream finishes.
    /// </summary>
    public async IAsyncEnumerable<string> StreamMessageAsync(
        Guid userId,
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 1-5: Chuẩn bị giống SendMessageAsync
        var session = await GetOrCreateSessionAsync(userId, request, cancellationToken);
        await SaveUserMessageAsync(userId, session.Id, request.Message, cancellationToken);
        var historyMessages = await _chatRepository.GetRecentMessagesAsync(session.Id, MaxHistoryMessages, cancellationToken);
        var enhancedQuery = await EnhanceQueryAsync(request.Message, cancellationToken);
        var queryEmbedding = await _embeddingService.EmbedAsync(enhancedQuery, cancellationToken);
        var relevantChunks = await _chatRepository.SearchSimilarChunksAsync(queryEmbedding, TopKChunks, request.DocumentIds, cancellationToken);
        var systemPrompt = BuildSystemPrompt(relevantChunks);
        var geminiHistory = BuildGeminiHistory(historyMessages);

        _logger.LogInformation("Streaming Gemini response. SessionId={SessionId}, ChunksFound={ChunksFound}", session.Id, relevantChunks.Count);

        // 6. Stream response
        var fullResponse = new StringBuilder();
        await foreach (var chunk in _geminiChatService.StreamGenerateAsync(systemPrompt, geminiHistory, cancellationToken))
        {
            fullResponse.Append(chunk);
            yield return chunk;
        }

        // 7. Lưu response đầy đủ vào DB sau khi stream xong (kèm RetrievedChunkIds)
        var completeReply = fullResponse.ToString();
        if (!string.IsNullOrWhiteSpace(completeReply))
        {
            var chunkIds = relevantChunks.Select(c => c.Id).ToList();
            await SaveAssistantMessageAsync(userId, session.Id, completeReply, chunkIds, cancellationToken);
        }
    }

    public async Task<List<ChatSessionSummaryDto>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _chatRepository.GetUserSessionsAsync(userId, cancellationToken);
        return sessions.Select(s => new ChatSessionSummaryDto
        {
            Id = s.Id,
            DocumentIds = s.SessionDocuments?.Select(sd => sd.DocumentId).ToList() ?? new List<Guid>(),
            Title = s.Title,
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task<List<ChatMessageDto>> GetSessionMessagesAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        // Verify session belongs to user
        var session = await _chatRepository.GetSessionAsync(sessionId, cancellationToken);
        if (session == null || session.UserId != userId)
        {
            return [];
        }

        var messages = await _chatRepository.GetRecentMessagesAsync(sessionId, 100, cancellationToken);
        return messages.Select(m => new ChatMessageDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task<bool> DeleteSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _chatRepository.GetSessionAsync(sessionId, cancellationToken);
        if (session == null || session.UserId != userId)
        {
            return false;
        }

        await _chatRepository.DeleteSessionAsync(sessionId, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteSessionsAsync(Guid userId, List<Guid> sessionIds, CancellationToken cancellationToken = default)
    {
        if (sessionIds == null || !sessionIds.Any()) return false;

        // Verify that all sessions belong to the user
        var sessions = await _chatRepository.GetUserSessionsAsync(userId, cancellationToken);
        var validSessionIds = sessions.Select(s => s.Id).Intersect(sessionIds).ToList();

        if (validSessionIds.Count == 0) return false;

        await _chatRepository.DeleteSessionsAsync(validSessionIds, cancellationToken);
        return true;
    }

    #region Private Helpers

    private async Task<ChatSession> GetOrCreateSessionAsync(Guid userId, ChatRequest request, CancellationToken cancellationToken)
    {
        if (request.SessionId.HasValue)
        {
            var existing = await _chatRepository.GetSessionAsync(request.SessionId.Value, cancellationToken);
            if (existing != null && existing.UserId == userId)
            {
                return existing;
            }
        }

        // Tạo session mới, lấy 50 ký tự đầu của message làm Title
        var title = request.Message.Length > 50
            ? request.Message[..50] + "..."
            : request.Message;

        var session = new ChatSession
        {
            UserId = userId,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            SessionDocuments = request.DocumentIds?.Select(id => new ChatSessionDocument { DocumentId = id }).ToList() ?? new List<ChatSessionDocument>()
        };

        return await _chatRepository.CreateSessionAsync(session, cancellationToken);
    }

    private async Task SaveUserMessageAsync(Guid userId, Guid sessionId, string content, CancellationToken cancellationToken)
    {
        var tokenCount = content.Length / 4 + 1;
        var message = new ChatMessage
        {
            SessionId = sessionId,
            Role = ChatRole.User,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            TokenCount = tokenCount
        };
        await _chatRepository.AddMessageAsync(message, cancellationToken);
        await _tokenUsageService.RecordChatTokensAsync(userId, tokenCount, cancellationToken);
    }

    private async Task SaveAssistantMessageAsync(Guid userId, Guid sessionId, string content, List<Guid>? retrievedChunkIds, CancellationToken cancellationToken)
    {
        var tokenCount = content.Length / 4 + 1;
        var message = new ChatMessage
        {
            SessionId = sessionId,
            Role = ChatRole.Assistant,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            RetrievedChunkIds = retrievedChunkIds ?? [],
            TokenCount = tokenCount
        };
        await _chatRepository.AddMessageAsync(message, cancellationToken);
        await _tokenUsageService.RecordChatTokensAsync(userId, tokenCount, cancellationToken);
    }

    /// <summary>
    /// Builds a strict System Prompt for RAG: grounded generation + source citation.
    /// </summary>
    private static string BuildSystemPrompt(List<DocumentChunk> chunks)
    {
        var contextBuilder = new StringBuilder();

        if (chunks.Count == 0)
        {
            contextBuilder.AppendLine("[CONTEXT]");
            contextBuilder.AppendLine("Không có tài liệu nào được cung cấp.");
            contextBuilder.AppendLine("[/CONTEXT]");
        }
        else
        {
            contextBuilder.AppendLine("[CONTEXT]");
            for (var i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var docTitle = chunk.Document?.Title ?? "N/A";
                var chapterTitle = chunk.Chapter?.Title ?? "N/A";
                var page = (chunk.PageNumber.HasValue && chunk.PageNumber.Value > 0)
                    ? chunk.PageNumber.Value.ToString()
                    : "không có";

                contextBuilder.AppendLine($"--- Chunk {i + 1} ---");
                contextBuilder.AppendLine($"Tài liệu: {docTitle}");
                contextBuilder.AppendLine($"Chương: {chapterTitle}");
                contextBuilder.AppendLine($"Trang: {page}");
                contextBuilder.AppendLine("Nội dung:");
                contextBuilder.AppendLine(chunk.Content);
                contextBuilder.AppendLine();
            }
            contextBuilder.AppendLine("[/CONTEXT]");
        }

        return BLL.Constants.PromptTemplates.RAG_SYSTEM_PROMPT.Replace("{context_chunks}", contextBuilder.ToString());
    }

    /// <summary>
    /// Converts a list of ChatMessages from the DB into the format required by the Gemini API.
    /// </summary>
    private static List<GeminiChatMessage> BuildGeminiHistory(List<ChatMessage> historyMessages)
    {
        return historyMessages
            .Where(m => m.Role is ChatRole.User or ChatRole.Assistant)
            .Select(m => new GeminiChatMessage
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToList();
    }

    /// <summary>
    /// Extracts source metadata from the retrieved chunks (deduplicated).
    /// </summary>
    private static List<ChatSourceDto> ExtractSources(List<DocumentChunk> chunks)
    {
        return chunks
            .Where(c => c.Document != null)
            .Select(c => new ChatSourceDto
            {
                DocumentId = c.DocumentId,
                DocumentTitle = c.Document!.Title,
                ChapterTitle = c.Chapter?.Title,
                PageNumber = c.PageNumber
            })
            .GroupBy(s => new { s.DocumentId, s.ChapterTitle, s.PageNumber })
            .Select(g => g.First())
            .ToList();
    }

    private static readonly HashSet<string> VietnameseUnmarkedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "la", "gi", "cua", "trong", "tren", "duoi", "va", "hoac", "de", "cho", "tai", "lieu", 
        "co", "khong", "nao", "dau", "the", "lam", "sao", "mot", "hai", "ba", "bon", "nam",
        "tim", "kiem", "thuat", "toan", "cau", "truc", "du", "lieu", "mang", "danh", "sach",
        "lien", "ket", "cay", "nhi", "phan", "do", "thi", "nhe", "nha", "oi", "dung", "sai"
    };

    private static bool IsProbablyVietnamese(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // 1. Kiểm tra ký tự tiếng Việt có dấu
        var hasVietnameseDiacritics = Regex.IsMatch(text, @"[áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸÝĐ]");
        if (hasVietnameseDiacritics) return true;

        // 2. Tách các từ riêng biệt và đối chiếu với từ điển tiếng Việt không dấu phổ biến
        var words = Regex.Split(text, @"\P{L}+");
        foreach (var word in words)
        {
            if (VietnameseUnmarkedWords.Contains(word))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string> EnhanceQueryAsync(string originalQuery, CancellationToken cancellationToken)
    {
        if (!IsProbablyVietnamese(originalQuery))
        {
            return originalQuery;
        }

        var systemPrompt = "You are a professional technical translator. Translate the user's computer science query from Vietnamese to English. Optimize the translation to be used for semantic vector search in English textbooks. Return ONLY the final translated English query, without any explanation, markdown, quotes or extra text.";
        var history = new List<GeminiChatMessage>
        {
            new GeminiChatMessage { Role = ChatRole.User, Content = originalQuery }
        };

        try
        {
            var englishQuery = await _geminiChatService.GenerateAsync(systemPrompt, history, cancellationToken);
            var cleaned = englishQuery.Trim().Trim('"', '\'', '`');
            
            if (!string.IsNullOrWhiteSpace(cleaned) && !cleaned.Equals(originalQuery, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Query enhanced: '{Original}' -> '{Original} | {Translated}'", originalQuery, originalQuery, cleaned);
                return $"{originalQuery} | {cleaned}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to translate query. Using original query.");
        }

        return originalQuery;
    }

    #endregion
}
