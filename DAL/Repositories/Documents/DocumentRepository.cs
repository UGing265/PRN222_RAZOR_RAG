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

    public Task<List<Document>> GetPendingDocumentsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        => _dbContext.Documents.AsNoTracking()
            .Include(x => x.DocumentFiles)
            .Include(x => x.DocumentChunks)
            .Where(x => x.OwnerUserId == ownerUserId && x.Status == "pending")
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

    public Task<Document?> GetDocumentBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => _dbContext.Documents.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

    public Task<Document?> GetDocumentBySlugAsync(string slug, Guid? requesterUserId, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking().Where(x => x.Slug == slug);
        if (requesterUserId is not null)
        {
            q = q.Where(x => x.Visibility != "private" || x.OwnerUserId == requesterUserId.Value);
        }
        else
        {
            q = q.Where(x => x.Visibility != "private");
        }
        return q.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.Documents
            .Include(x => x.DocumentFiles)
            .Include(x => x.DocumentChunks)
            .Include(x => x.DocumentChapters)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

    public Task<Document?> GetDocumentWithFilesBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => _dbContext.Documents
            .Include(x => x.DocumentFiles)
            .Include(x => x.DocumentChunks)
            .Include(x => x.DocumentChapters)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

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

    public Task<List<Document>> GetDocumentsByOwnerAsync(Guid ownerUserId, string? query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking()
            .Include(x => x.DocumentFiles)
            .Include(x => x.DocumentChunks)
            .Where(x => x.OwnerUserId == ownerUserId && x.Status == "completed");

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(x => x.Title.Contains(query) || (x.Subject != null && x.Subject.Contains(query)) || (x.School != null && x.School.Contains(query)));
        }

        return q.OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountDocumentsByOwnerAsync(Guid ownerUserId, string? query, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking().Where(x => x.OwnerUserId == ownerUserId && x.Status == "completed");
        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(x => x.Title.Contains(query) || (x.Subject != null && x.Subject.Contains(query)) || (x.School != null && x.School.Contains(query)));
        }
        return q.CountAsync(cancellationToken);
    }

    public Task<List<Document>> GetDocumentsAsync(string? query, int page, int pageSize, Guid? requesterUserId = null, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking()
            .Include(x => x.DocumentFiles)
            .Include(x => x.DocumentChunks)
            .Include(x => x.OwnerUser)
            .Where(x => x.Status == "completed" && (x.OwnerUser.RoleId == 1 || x.OwnerUser.RoleId == 2));

        q = q.Where(x => x.Visibility != "private");

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(x => x.Title.Contains(query) || (x.Subject != null && x.Subject.Contains(query)) || (x.School != null && x.School.Contains(query)) || (x.Description != null && x.Description.Contains(query)));
        }

        return q.OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountDocumentsAsync(string? query, Guid? requesterUserId = null, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking()
            .Include(x => x.OwnerUser)
            .Where(x => x.Status == "completed" && (x.OwnerUser.RoleId == 1 || x.OwnerUser.RoleId == 2));

        q = q.Where(x => x.Visibility != "private");

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(x => x.Title.Contains(query) || (x.Subject != null && x.Subject.Contains(query)) || (x.School != null && x.School.Contains(query)) || (x.Description != null && x.Description.Contains(query)));
        }
        return q.CountAsync(cancellationToken);
    }

    public Task<int> CountDocumentsByStatusAsync(Guid ownerUserId, string status, CancellationToken cancellationToken = default)
        => _dbContext.Documents.CountAsync(x => x.OwnerUserId == ownerUserId && x.Status == status, cancellationToken);

    public Task<int> CountFilesByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        => _dbContext.DocumentFiles.CountAsync(x => x.Document.OwnerUserId == ownerUserId, cancellationToken);

    public Task<int> CountChunksByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        => _dbContext.DocumentChunks.CountAsync(x => x.Document.OwnerUserId == ownerUserId, cancellationToken);

    public Task<List<UploadJob>> GetActiveUploadJobsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        => _dbContext.UploadJobs.AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId && x.Status == "processing")
            .OrderByDescending(x => x.UpdatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

    public Task<Document?> GetOwnedDocumentAsync(Guid documentId, Guid ownerUserId, CancellationToken cancellationToken = default)
        => _dbContext.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId && x.OwnerUserId == ownerUserId, cancellationToken);

    public Task<Document?> GetOwnedDocumentBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default)
        => _dbContext.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.OwnerUserId == ownerUserId, cancellationToken);

    public Task<int> CountFilesByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.DocumentFiles.CountAsync(x => x.DocumentId == documentId, cancellationToken);

    public Task<int> CountChunksByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.DocumentChunks.CountAsync(x => x.DocumentId == documentId, cancellationToken);

    public async Task AddDocumentFilesAsync(IEnumerable<DocumentFile> files, CancellationToken cancellationToken = default)
        => await _dbContext.DocumentFiles.AddRangeAsync(files, cancellationToken);

    public async Task AddDocumentChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        => await _dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);

    public async Task AddDocumentChaptersAsync(IEnumerable<DocumentChapter> chapters, CancellationToken cancellationToken = default)
        => await _dbContext.DocumentChapters.AddRangeAsync(chapters, cancellationToken);

    public async Task RemoveUploadJobsByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var jobs = await _dbContext.UploadJobs.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken);
        _dbContext.UploadJobs.RemoveRange(jobs);
    }

    public async Task RemoveDocumentFilesByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var files = await _dbContext.DocumentFiles.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken);
        _dbContext.DocumentFiles.RemoveRange(files);
    }

    public async Task RemoveDocumentChunksByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var chunks = await _dbContext.DocumentChunks.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken);
        _dbContext.DocumentChunks.RemoveRange(chunks);
    }

    public async Task RemoveDocumentChaptersByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var chapters = await _dbContext.DocumentChapters.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken);
        _dbContext.DocumentChapters.RemoveRange(chapters);
    }

    public Task RemoveDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        _dbContext.Documents.Remove(document);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
