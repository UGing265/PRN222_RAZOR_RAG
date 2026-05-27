using DAL.Entities;

namespace BLL.Interfaces.Documents;

public interface IUploadProcessingService
{
    Task ProcessAsync(UploadJob job, CancellationToken cancellationToken = default);
}
