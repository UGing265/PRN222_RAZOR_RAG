using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces.Documents;

public interface IS3StorageService
{
    Task<(string Key, string Url)> UploadAsync(string documentId, IFormFile file, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
