using Pgvector;

namespace BLL.Interfaces.Documents;

public interface IEmbeddingService
{
    Task<Vector> EmbedAsync(string text, CancellationToken cancellationToken = default);
}
