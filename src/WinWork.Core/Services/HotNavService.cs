using WinWork.Core.Interfaces;
using WinWork.Data.Repositories;
using WinWork.Models;

namespace WinWork.Core.Services;

public class HotNavService : IHotNavService
{
    private readonly IHotNavRepository _repo;

    public HotNavService(IHotNavRepository repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<HotNav>> GetAllAsync() => _repo.GetAllAsync();

    public Task<HotNav?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public Task<HotNav> CreateAsync(HotNav hotNav) => _repo.CreateAsync(hotNav);

    public Task<HotNav> UpdateAsync(HotNav hotNav) => _repo.UpdateAsync(hotNav);

    public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
}
