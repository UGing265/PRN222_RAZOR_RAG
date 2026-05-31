using DAL.Entities;

namespace DAL.Interfaces.Documents;

public interface IUploadJobRepository
{
    Task<UploadJob> AddUploadJobAsync(UploadJob job, CancellationToken cancellationToken = default);
    Task<UploadJob?> GetNextPendingJobAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
