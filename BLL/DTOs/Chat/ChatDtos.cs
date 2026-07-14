namespace BLL.DTOs.Chat;

public sealed class ChatRequest
{
    public Guid? SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Guid>? DocumentIds { get; set; }
}

public sealed class ChatResponse
{
    public Guid SessionId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public string Reply { get; set; } = string.Empty;
    public List<ChatSourceDto> Sources { get; set; } = [];
}

public sealed class ChatSourceDto
{
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string? ChapterTitle { get; set; }
    public int? PageNumber { get; set; }
    public string? ContentSnippet { get; set; }
    public int? ChunkIndex { get; set; }
    public string DocumentSlug { get; set; } = string.Empty;
    public int? ChunkOrder { get; set; }
}

public sealed class ChatSessionSummaryDto
{
    public Guid Id { get; set; }
    public List<Guid> DocumentIds { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ChatSourceDto> Sources { get; set; } = [];
}

/// <summary>
/// Represents a single message in the Gemini Chat API conversation history.
/// </summary>
public sealed class GeminiChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Result of comparing multiple documents via LLM.
/// Field names match the COMPARISON_PROMPT JSON schema exactly for direct deserialization.
/// </summary>
public sealed class ComparisonResultDto
{
    public decimal SimilarityPercentage { get; set; }
    public string SimilarityExplanation { get; set; } = string.Empty;
    public List<string> SimilarPoints { get; set; } = [];
    public Dictionary<string, List<string>> DifferentPoints { get; set; } = new();
}
