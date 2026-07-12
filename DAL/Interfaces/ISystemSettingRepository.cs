namespace DAL.Interfaces;

using DAL.Entities;

public interface ISystemSettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
    Task SetValueAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default);
    Task SetValuesAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default);
}
