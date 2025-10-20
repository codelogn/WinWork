using WinWork.Models;

namespace WinWork.Data.Repositories;

/// <summary>
/// Repository interface for AppSettings entities
/// </summary>
public interface ISettingsRepository
{
    Task<IEnumerable<AppSettings>> GetAllAsync();
    Task<AppSettings?> GetByKeyAsync(string key);
    Task<string?> GetValueAsync(string key);
    Task<T?> GetValueAsync<T>(string key) where T : struct;
    Task<bool> SetValueAsync(string key, string value);
    Task<bool> SetValueAsync<T>(string key, T value) where T : struct;
    Task<AppSettings> CreateAsync(AppSettings settings);
    Task<AppSettings> UpdateAsync(AppSettings settings);
    Task<bool> DeleteAsync(string key);
}
