using WinWork.Models;

namespace WinWork.Core.Interfaces;

public interface IHotNavService
{
    Task<IEnumerable<HotNav>> GetAllAsync();
    Task<HotNav?> GetByIdAsync(int id);
    Task<HotNav> CreateAsync(HotNav hotNav);
    Task<HotNav> UpdateAsync(HotNav hotNav);
    Task<bool> DeleteAsync(int id);
}
