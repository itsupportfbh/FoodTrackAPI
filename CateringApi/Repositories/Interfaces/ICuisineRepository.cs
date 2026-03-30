using CateringApi.Models;

namespace CateringApi.Repositories.Interfaces
{
    public interface ICuisineRepository
    {
        Task<IEnumerable<CuisineDto>> GetAllAsync();
        Task<CuisineDto?> GetByIdAsync(int id);
        Task<int> SaveAsync(Cuisine dto);
        Task<bool> DeleteAsync(int id, int? userId);
        Task<CuisineDto?> GetByNameAsync(string cuisineName);
        Task<bool> NameExistsAsync(string cuisineName, int excludeId);
    }
}