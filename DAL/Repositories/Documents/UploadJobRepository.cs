using DAL.Data;
using DAL.Entities;
using DAL.Interfaces.Documents;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Documents;

public class UploadJobRepository : IUploadJobRepository
{
    private readonly DBContext _dbContext;

    public UploadJobRepository(DBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UploadJob?> GetNextPendingJobAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadJobs
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Status == "pending", cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
