using DAL.Entities;

namespace BLL.Models.Documents;

public class DocumentProcessingResult
{
    public required DocumentFile DocumentFile { get; init; }
    public required IReadOnlyList<DocumentChunk> Chunks { get; init; }
}
