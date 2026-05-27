using DAL.Entities;

namespace BLL.Interfaces.Documents;

public interface IChapterSegmentationService
{
    Task<List<DocumentChapter>> GenerateChaptersAsync(Document document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);
}
