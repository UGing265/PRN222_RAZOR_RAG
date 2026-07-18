using BLL.Interfaces;
using BLL.Services.Documents;
using DAL.Interfaces;

namespace BLL.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly ISystemSettingRepository _repository;

    public SystemSettingService(ISystemSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<DocumentIndexingOptions> GetChunkingSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAllSettingsAsync(cancellationToken);
        var options = new DocumentIndexingOptions();

        if (settings.TryGetValue("ChunkMinWords", out var minWordsStr) && int.TryParse(minWordsStr, out var minWords))
        {
            options.ChunkMinWords = minWords;
        }

        if (settings.TryGetValue("ChunkMaxWords", out var maxWordsStr) && int.TryParse(maxWordsStr, out var maxWords))
        {
            options.ChunkMaxWords = maxWords;
        }

        if (settings.TryGetValue("ChunkOverlapWords", out var overlapWordsStr) && int.TryParse(overlapWordsStr, out var overlapWords))
        {
            options.ChunkOverlapWords = overlapWords;
        }

        return options;
    }

    public async Task UpdateChunkingSettingsAsync(int minWords, int maxWords, int overlapWords, CancellationToken cancellationToken = default)
    {
        var updates = new Dictionary<string, string>
        {
            { "ChunkMinWords", minWords.ToString() },
            { "ChunkMaxWords", maxWords.ToString() },
            { "ChunkOverlapWords", overlapWords.ToString() }
        };

        await _repository.SetValuesAsync(updates, cancellationToken);
    }
    public async Task<int> GetStudentDailyTokenLimitAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAllSettingsAsync(cancellationToken);
        if (settings.TryGetValue("DailyTokenLimit_Student", out var limitStr) && int.TryParse(limitStr, out var limit))
        {
            return limit;
        }
        return 0; // 0 indicates unlimited if not configured
    }

    public async Task UpdateStudentDailyTokenLimitAsync(int dailyTokenLimit, CancellationToken cancellationToken = default)
    {
        var updates = new Dictionary<string, string>
        {
            { "DailyTokenLimit_Student", dailyTokenLimit.ToString() }
        };
        await _repository.SetValuesAsync(updates, cancellationToken);
    }

    public async Task<int> GetLecturerDailyTokenLimitAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAllSettingsAsync(cancellationToken);
        if (settings.TryGetValue("DailyTokenLimit_Lecturer", out var limitStr) && int.TryParse(limitStr, out var limit))
        {
            return limit;
        }
        return 0; // 0 indicates unlimited if not configured
    }

    public async Task UpdateLecturerDailyTokenLimitAsync(int dailyTokenLimit, CancellationToken cancellationToken = default)
    {
        var updates = new Dictionary<string, string>
        {
            { "DailyTokenLimit_Lecturer", dailyTokenLimit.ToString() }
        };
        await _repository.SetValuesAsync(updates, cancellationToken);
    }
}
