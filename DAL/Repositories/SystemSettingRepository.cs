using DAL.Data;
using DAL.Entities;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly DBContext _context;

    public SystemSettingRepository(DBContext context)
    {
        _context = context;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        
        return setting?.Value;
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
    }

    public async Task SetValueAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting == null)
        {
            setting = new SystemSetting
            {
                Key = key,
                Value = value,
                Description = description
            };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            if (description != null)
            {
                setting.Description = description;
            }
            setting.UpdatedAt = DateTime.UtcNow;
            _context.SystemSettings.Update(setting);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetValuesAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        foreach (var kvp in settings)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == kvp.Key, cancellationToken);
            if (setting == null)
            {
                _context.SystemSettings.Add(new SystemSetting { Key = kvp.Key, Value = kvp.Value });
            }
            else
            {
                setting.Value = kvp.Value;
                setting.UpdatedAt = DateTime.UtcNow;
                _context.SystemSettings.Update(setting);
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}
