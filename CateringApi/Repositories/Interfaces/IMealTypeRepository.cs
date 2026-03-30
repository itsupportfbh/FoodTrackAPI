using CateringApi.DTOs.MealType;

namespace CateringApi.Repositories.Interfaces
{
    public interface IMealTypeRepository
    {
        Task<IEnumerable<MealTypeDto>> GetAllAsync();
    }
}