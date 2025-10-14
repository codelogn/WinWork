using Microsoft.EntityFrameworkCore;
using LinkerApp.Models;
using System.Globalization;

namespace LinkerApp.Data.Repositories;

/// <summary>
/// Repository implementation for AppSettings entities
/// </summary>
public class SettingsRepository : ISettingsRepository
{
    private readonly LinkerAppDbContext _context;

    public SettingsRepository(LinkerAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AppSettings>> GetAllAsync()
    {
        return await _context.AppSettings
            .OrderBy(s => s.Key)
            .ToListAsync();
    }

    public async Task<AppSettings?> GetByKeyAsync(string key)
    {
        return await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await GetByKeyAsync(key);
        return setting?.Value;
    }

    public async Task<T?> GetValueAsync<T>(string key) where T : struct
    {
        var value = await GetValueAsync(key);
        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(value, out var boolResult))
                    return (T)(object)boolResult;
            }
            else if (typeof(T) == typeof(int))
            {
                if (int.TryParse(value, out var intResult))
                    return (T)(object)intResult;
            }
            else if (typeof(T) == typeof(double))
            {
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleResult))
                    return (T)(object)doubleResult;
            }
            else if (typeof(T) == typeof(DateTime))
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateResult))
                    return (T)(object)dateResult;
            }
        }
        catch
        {
            // Return null if conversion fails
        }

        return null;
    }

    public async Task<bool> SetValueAsync(string key, string value)
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null)
        {
            setting = new AppSettings
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            };
            _context.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            _context.AppSettings.Update(setting);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetValueAsync<T>(string key, T value) where T : struct
    {
        string stringValue;
        
        if (typeof(T) == typeof(bool))
        {
            stringValue = value.ToString()?.ToLower() ?? "false";
        }
        else if (typeof(T) == typeof(double))
        {
            stringValue = ((double)(object)value).ToString(CultureInfo.InvariantCulture);
        }
        else if (typeof(T) == typeof(DateTime))
        {
            stringValue = ((DateTime)(object)value).ToString("O", CultureInfo.InvariantCulture);
        }
        else
        {
            stringValue = value.ToString() ?? string.Empty;
        }

        return await SetValueAsync(key, stringValue);
    }

    public async Task<AppSettings> CreateAsync(AppSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.AppSettings.Add(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task<AppSettings> UpdateAsync(AppSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.AppSettings.Update(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task<bool> DeleteAsync(string key)
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null)
            return false;

        _context.AppSettings.Remove(setting);
        await _context.SaveChangesAsync();
        return true;
    }
}