namespace BLL.Exceptions;

/// <summary>
/// Thrown when the Gemini Embedding API fails after exhausting all retries.
/// </summary>
public class EmbeddingFailedException : Exception
{
    public EmbeddingFailedException(string message) : base(message) { }
    public EmbeddingFailedException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when all selected documents are not in a valid status for chat (Completed/Approved).
/// </summary>
public class DocumentNotReadyException : Exception
{
    public List<Guid> DocumentIds { get; }

    public DocumentNotReadyException(List<Guid> documentIds)
        : base($"Tài liệu chưa sẵn sàng để chat. DocumentIds: [{string.Join(", ", documentIds)}]")
    {
        DocumentIds = documentIds;
    }

    public DocumentNotReadyException(string message) : base(message)
    {
        DocumentIds = [];
    }
}

/// <summary>
/// Thrown when the LLM response cannot be parsed into the expected DTO (e.g., ComparisonResultDto JSON).
/// </summary>
public class LlmResponseParsingException : Exception
{
    public string RawResponse { get; }

    public LlmResponseParsingException(string message, string rawResponse)
        : base(message)
    {
        RawResponse = rawResponse;
    }

    public LlmResponseParsingException(string message, string rawResponse, Exception innerException)
        : base(message, innerException)
    {
        RawResponse = rawResponse;
    }
}

/// <summary>
/// Thrown when the vector similarity search in pgvector fails.
/// </summary>
public class SemanticSearchException : Exception
{
    public SemanticSearchException(string message) : base(message) { }
    public SemanticSearchException(string message, Exception innerException) : base(message, innerException) { }
}
