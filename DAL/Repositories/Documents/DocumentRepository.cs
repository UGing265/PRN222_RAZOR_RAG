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

    public DBContext GetDbContext() => _dbContext;

    public async Task<Document> AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return document;
    }

    public Task<bool> ExistsDocumentByMd5Async(string md5Hash, CancellationToken cancellationToken = default)
        => _dbContext.Documents.AsNoTracking()
            .AnyAsync(x => x.Md5Hash == md5Hash, cancellationToken);

    public Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.Documents
            
            .Include(x => x.Subject)
            .Include(x => x.DocumentType)
            .Include(x => x.Language)
            .Include(x => x.AcademicTerm)
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

    public Task<List<Document>> GetPendingDocumentsByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        => _dbContext.Documents.AsNoTracking()
            .Include(x => x.DocumentFiles)
            .Include(x => x.DocumentChunks)
            
            .Include(x => x.Subject)
            .Include(x => x.DocumentType)
            .Include(x => x.Language)
            .Include(x => x.AcademicTerm)
            .Where(x => x.OwnerUserId == ownerUserId && x.Status == "pending")
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

    public Task<Document?> GetDocumentBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => _dbContext.Documents
            
            .Include(x => x.Subject)
            .Include(x => x.DocumentType)
            .Include(x => x.Language)
            .Include(x => x.AcademicTerm)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

    public Task<Document?> GetDocumentBySlugAsync(string slug, Guid? requesterUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking()
            
            .Include(x => x.Subject)
            .Include(x => x.DocumentType)
            .Include(x => x.Language)
            .Include(x => x.AcademicTerm)
            .Where(x => x.Slug == slug);
            
        if (!isAdmin)
        {
            if (requesterUserId is not null)
            {
                q = q.Where(x => x.Visibility != "private" || x.OwnerUserId == requesterUserId.Value);
            }
            else
            {
                q = q.Where(x => x.Visibility != "private");
            }
        }
        return q.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Document?> GetDocumentWithFilesAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _dbContext.Documents
            .Include(x => x.DocumentFiles)
            .Include(x => x.DocumentChunks)
            .Include(x => x.DocumentChapters)
            
            .Include(x => x.Subject)
            .Include(x => x.DocumentType)
            .Include(x => x.Language)
            .Include(x => x.AcademicTerm)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

    public Task<Document?> GetDocumentWithFilesBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => _dbContext.Documents
            .Include(x => x.DocumentFiles)
            .Include(x => x.DocumentChunks)
            .Include(x => x.DocumentChapters)
            
            .Include(x => x.Subject)
            .Include(x => x.DocumentType)
            .Include(x => x.Language)
            .Include(x => x.AcademicTerm)
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

    public Task<List<Document>> GetDocumentsByOwnerAsync(Guid ownerUserId, string? query, Guid? subjectId, Guid? termId, string? sortBy, Guid? documentTypeId, Guid? languageId, Guid? documentSourceId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking()
            .Include(x => x.DocumentFiles)
            
            .Include(x => x.Subject)
            .Include(x => x.DocumentType)
            .Include(x => x.Language)
            .Include(x => x.AcademicTerm)
            .Where(x => x.OwnerUserId == ownerUserId && x.Status == "completed");

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(x => x.Title.Contains(query) || (x.Subject != null && x.Subject.Name.Contains(query)));
        }

        if (subjectId.HasValue)
        {
            q = q.Where(x => x.SubjectId == subjectId.Value);
        }

        if (termId.HasValue)
        {
            q = q.Where(x => x.AcademicTermId == termId.Value);
        }

        if (documentTypeId.HasValue)
        {
            q = q.Where(x => x.DocumentTypeId == documentTypeId.Value);
        }

        if (languageId.HasValue)
        {
            q = q.Where(x => x.LanguageId == languageId.Value);
        }

        if (documentSourceId.HasValue)
        {
            q = q.Where(x => x.DocumentSourceId == documentSourceId.Value);
        }

        q = sortBy switch
        {
            "title_asc" => q.OrderBy(x => x.Title),
            "title_desc" => q.OrderByDescending(x => x.Title),
            "date_asc" => q.OrderBy(x => x.CreatedAt),
            "date_desc" => q.OrderByDescending(x => x.CreatedAt),
            "views_asc" => q.OrderBy(x => x.ViewCount),
            "views_desc" => q.OrderByDescending(x => x.ViewCount),
            _ => q.OrderByDescending(x => x.UpdatedAt)
        };

        return q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountDocumentsByOwnerAsync(Guid ownerUserId, string? query, Guid? subjectId, Guid? termId, Guid? documentTypeId, Guid? languageId, Guid? documentSourceId, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking()
            
            .Include(x => x.Subject)
            .Where(x => x.OwnerUserId == ownerUserId && x.Status == "completed");

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(x => x.Title.Contains(query) || (x.Subject != null && x.Subject.Name.Contains(query)));
        }

        if (subjectId.HasValue)
        {
            q = q.Where(x => x.SubjectId == subjectId.Value);
        }

        if (termId.HasValue)
        {
            q = q.Where(x => x.AcademicTermId == termId.Value);
        }

        if (documentTypeId.HasValue)
        {
            q = q.Where(x => x.DocumentTypeId == documentTypeId.Value);
        }

        if (languageId.HasValue)
        {
            q = q.Where(x => x.LanguageId == languageId.Value);
        }

        if (documentSourceId.HasValue)
        {
            q = q.Where(x => x.DocumentSourceId == documentSourceId.Value);
        }

        return q.CountAsync(cancellationToken);
    }

    public Task<List<Document>> GetDocumentsAsync(string? query, Guid? subjectId, int page, int pageSize, Guid? requesterUserId = null, string? sortBy = null, Guid? documentTypeId = null, Guid? languageId = null, Guid? documentSourceId = null, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking()
            .Include(x => x.DocumentFiles)
            .Include(x => x.OwnerUser)
            
            .Include(x => x.Subject)
            .Include(x => x.DocumentType)
            .Include(x => x.Language)
            .Include(x => x.AcademicTerm)
            .Where(x => x.Status == "completed" && (x.OwnerUser.RoleId == 1 || x.OwnerUser.RoleId == 2));

        q = q.Where(x => x.Visibility != "private");

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(x => x.Title.Contains(query) || (x.Subject != null && x.Subject.Name.Contains(query)) || (x.Description != null && x.Description.Contains(query)));
        }

        if (subjectId.HasValue)
        {
            q = q.Where(x => x.SubjectId == subjectId.Value);
        }

        if (documentTypeId.HasValue)
        {
            q = q.Where(x => x.DocumentTypeId == documentTypeId.Value);
        }

        if (languageId.HasValue)
        {
            q = q.Where(x => x.LanguageId == languageId.Value);
        }

        if (documentSourceId.HasValue)
        {
            q = q.Where(x => x.DocumentSourceId == documentSourceId.Value);
        }

        if (string.Equals(sortBy, "title", StringComparison.OrdinalIgnoreCase) || string.Equals(sortBy, "title_asc", StringComparison.OrdinalIgnoreCase))
        {
            q = q.OrderBy(x => x.Title);
        }
        else if (string.Equals(sortBy, "title_desc", StringComparison.OrdinalIgnoreCase))
        {
            q = q.OrderByDescending(x => x.Title);
        }
        else if (string.Equals(sortBy, "views", StringComparison.OrdinalIgnoreCase) || string.Equals(sortBy, "views_desc", StringComparison.OrdinalIgnoreCase))
        {
            q = q.OrderByDescending(x => x.ViewCount);
        }
        else if (string.Equals(sortBy, "views_asc", StringComparison.OrdinalIgnoreCase))
        {
            q = q.OrderBy(x => x.ViewCount);
        }
        else if (string.Equals(sortBy, "date", StringComparison.OrdinalIgnoreCase) || string.Equals(sortBy, "date_desc", StringComparison.OrdinalIgnoreCase))
        {
            q = q.OrderByDescending(x => x.CreatedAt);
        }
        else if (string.Equals(sortBy, "date_asc", StringComparison.OrdinalIgnoreCase))
        {
            q = q.OrderBy(x => x.CreatedAt);
        }
        else
        {
            q = q.OrderByDescending(x => x.UpdatedAt);
        }

        return q.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountDocumentsAsync(string? query, Guid? subjectId, Guid? requesterUserId = null, Guid? documentTypeId = null, Guid? languageId = null, Guid? documentSourceId = null, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking()
            .Include(x => x.OwnerUser)
            
            .Include(x => x.Subject)
            .Where(x => x.Status == "completed" && (x.OwnerUser.RoleId == 1 || x.OwnerUser.RoleId == 2));

        q = q.Where(x => x.Visibility != "private");

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(x => x.Title.Contains(query) || (x.Subject != null && x.Subject.Name.Contains(query)) || (x.Description != null && x.Description.Contains(query)));
        }

        if (subjectId.HasValue)
        {
            q = q.Where(x => x.SubjectId == subjectId.Value);
        }

        if (documentTypeId.HasValue)
        {
            q = q.Where(x => x.DocumentTypeId == documentTypeId.Value);
        }

        if (languageId.HasValue)
        {
            q = q.Where(x => x.LanguageId == languageId.Value);
        }

        if (documentSourceId.HasValue)
        {
            q = q.Where(x => x.DocumentSourceId == documentSourceId.Value);
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
        => _dbContext.Documents
            
            .Include(x => x.Subject)
            .Include(x => x.AcademicTerm)
            .FirstOrDefaultAsync(x => x.Id == documentId && x.OwnerUserId == ownerUserId, cancellationToken);

    public Task<Document?> GetOwnedDocumentBySlugAsync(string slug, Guid ownerUserId, CancellationToken cancellationToken = default)
        => _dbContext.Documents
            
            .Include(x => x.Subject)
            .Include(x => x.AcademicTerm)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.OwnerUserId == ownerUserId, cancellationToken);

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

    public async Task<Dictionary<Guid, string>> GetPreviewTextsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        if (documentIds == null || !documentIds.Any())
            return new Dictionary<Guid, string>();

        return await _dbContext.DocumentChunks
            .Where(x => documentIds.Contains(x.DocumentId) && x.ChunkOrder == 0)
            .Select(x => new { x.DocumentId, x.Content })
            .ToDictionaryAsync(x => x.DocumentId, x => x.Content, cancellationToken);
    }



    public Task<List<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default)
        => _dbContext.Subjects.AsNoTracking().Include(x => x.AcademicTerm).OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public Task<List<DocumentType>> GetDocumentTypesAsync(CancellationToken cancellationToken = default)
        => _dbContext.DocumentTypes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<List<Language>> GetLanguagesAsync(CancellationToken cancellationToken = default)
        => _dbContext.Languages.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<List<DocumentSource>> GetDocumentSourcesAsync(CancellationToken cancellationToken = default)
        => _dbContext.DocumentSources.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<List<AcademicTerm>> GetAcademicTermsAsync(CancellationToken cancellationToken = default)
        => _dbContext.AcademicTerms.AsNoTracking().Include(x => x.Subjects).OrderBy(x => x.Order).ToListAsync(cancellationToken);

    public async Task<Subject> AddSubjectAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        await _dbContext.Subjects.AddAsync(subject, cancellationToken);
        return subject;
    }

    public Task<Subject?> GetSubjectAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        _dbContext.Subjects.Update(subject);
        return Task.CompletedTask;
    }

    public Task DeleteSubjectAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        _dbContext.Subjects.Remove(subject);
        return Task.CompletedTask;
    }

    public async Task<DocumentType> AddDocumentTypeAsync(DocumentType documentType, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentTypes.AddAsync(documentType, cancellationToken);
        return documentType;
    }

    public async Task<Language> AddLanguageAsync(Language language, CancellationToken cancellationToken = default)
    {
        await _dbContext.Languages.AddAsync(language, cancellationToken);
        return language;
    }

    public async Task<DocumentSource> AddDocumentSourceAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentSources.AddAsync(source, cancellationToken);
        return source;
    }

    public async Task<AcademicTerm> AddAcademicTermAsync(AcademicTerm term, CancellationToken cancellationToken = default)
    {
        await _dbContext.AcademicTerms.AddAsync(term, cancellationToken);
        return term;
    }

    public Task<AcademicTerm?> GetAcademicTermAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.AcademicTerms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task UpdateAcademicTermAsync(AcademicTerm term, CancellationToken cancellationToken = default)
    {
        _dbContext.AcademicTerms.Update(term);
        return Task.CompletedTask;
    }

    public Task DeleteAcademicTermAsync(AcademicTerm term, CancellationToken cancellationToken = default)
    {
        _dbContext.AcademicTerms.Remove(term);
        return Task.CompletedTask;
    }

    public Task<List<Document>> GetAdminDocumentsAsync(string? query, Guid? subjectId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<Document> q = _dbContext.Documents.AsNoTracking()
            .Include(x => x.DocumentFiles)
            .Include(x => x.OwnerUser)
            
            .Include(x => x.Subject)
            .Include(x => x.DocumentType)
            .Include(x => x.Language);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.Trim().ToLower();
            q = q.Where(x => x.Title.ToLower().Contains(lowerQuery) 
                || (x.Subject != null && x.Subject.Name.ToLower().Contains(lowerQuery))
                || (x.Subject != null && x.Subject.Code.ToLower().Contains(lowerQuery))
                || (x.OwnerUser != null && x.OwnerUser.Email.ToLower().Contains(lowerQuery))
                || (x.OwnerUser != null && x.OwnerUser.FullName.ToLower().Contains(lowerQuery)));
        }

        if (subjectId.HasValue)
        {
            q = q.Where(x => x.SubjectId == subjectId.Value);
        }

        return q.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminDocumentsAsync(string? query, Guid? subjectId, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Documents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.Trim().ToLower();
            q = q.Where(x => x.Title.ToLower().Contains(lowerQuery) 
                || (x.Subject != null && x.Subject.Name.ToLower().Contains(lowerQuery))
                || (x.Subject != null && x.Subject.Code.ToLower().Contains(lowerQuery))
                || (x.OwnerUser != null && x.OwnerUser.Email.ToLower().Contains(lowerQuery))
                || (x.OwnerUser != null && x.OwnerUser.FullName.ToLower().Contains(lowerQuery)));
        }

        if (subjectId.HasValue)
        {
            q = q.Where(x => x.SubjectId == subjectId.Value);
        }

        return q.CountAsync(cancellationToken);
    }

    public async Task<DocumentReport> AddDocumentReportAsync(DocumentReport report, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentReports.AddAsync(report, cancellationToken);
        return report;
    }

    public Task<List<DocumentReport>> GetPendingReportsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.DocumentReports.AsNoTracking()
            .Include(x => x.Document)
            .Include(x => x.ReporterUser)
            .Where(x => x.Status == "pending")
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<DocumentReport?> GetDocumentReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DocumentReports
            .Include(x => x.Document)
            .Include(x => x.ReporterUser)
            .FirstOrDefaultAsync(x => x.Id == reportId, cancellationToken);
    }

    public Task<List<DocumentReport>> GetDocumentReportsByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DocumentReports
            .Where(x => x.DocumentId == documentId)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveDocumentReportsByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var reports = await _dbContext.DocumentReports.Where(x => x.DocumentId == documentId).ToListAsync(cancellationToken);
        _dbContext.DocumentReports.RemoveRange(reports);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
