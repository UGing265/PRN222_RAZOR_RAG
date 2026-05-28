using DAL.Data;
using DAL.Entities;
using DAL.Interfaces.Documents;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Documents;

public class DocumentRepository : IDocumentRepository
{
    private readonly DBContext _dbContext;

    public DocumentRepository(DBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Document> AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return document;
    }

    public Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.Documents.FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

    public Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.Documents
            .Include(x => x.DocumentFiles)
            .Include(x => x.DocumentChunks)
            .Include(x => x.DocumentChapters)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

    public Task<Document?> GetDocumentWithChunksAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.Documents
            .Include(x => x.DocumentChunks)
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

    public Task<List<DocumentFile>> GetDocumentFilesAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.DocumentFiles.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken);

    public Task<List<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.DocumentChunks.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken);

    public Task<List<DocumentChapter>> GetDocumentChaptersAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.DocumentChapters.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken);

    public async Task AddDocumentFilesAsync(IEnumerable<DocumentFile> files, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentFiles.AddRangeAsync(files, cancellationToken);
    }

    public async Task AddDocumentChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
    }

    public async Task AddDocumentChaptersAsync(IEnumerable<DocumentChapter> chapters, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentChapters.AddRangeAsync(chapters, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
