using BLL.Services.Documents;

namespace BLL.Interfaces;

public interface ISystemSettingService
{
    Task<DocumentIndexingOptions> GetChunkingSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateChunkingSettingsAsync(int minWords, int maxWords, int overlapWords, CancellationToken cancellationToken = default);
}
