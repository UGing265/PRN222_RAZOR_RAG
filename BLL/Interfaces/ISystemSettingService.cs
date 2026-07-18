using BLL.Services.Documents;

namespace BLL.Interfaces;

public interface ISystemSettingService
{
    Task<DocumentIndexingOptions> GetChunkingSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateChunkingSettingsAsync(int minWords, int maxWords, int overlapWords, CancellationToken cancellationToken = default);
    Task<int> GetStudentDailyTokenLimitAsync(CancellationToken cancellationToken = default);
    Task UpdateStudentDailyTokenLimitAsync(int dailyTokenLimit, CancellationToken cancellationToken = default);
    Task<int> GetLecturerDailyTokenLimitAsync(CancellationToken cancellationToken = default);
    Task UpdateLecturerDailyTokenLimitAsync(int dailyTokenLimit, CancellationToken cancellationToken = default);
}
