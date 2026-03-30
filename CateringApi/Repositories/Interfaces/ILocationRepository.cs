using CateringApi.DTOs.Location;

namespace CateringApi.Repositories.Interfaces
{
    public interface ILocationRepository
    {
        Task<IEnumerable<LocationDTO>> GetAllLocation();
        Task<LocationDTO> GetLocationById(long id);
        Task<int> CreateLocation(LocationDTO locationDTO);
        Task UpdateLocation(LocationDTO locationDto);
        Task DeleteLocation(int id);
    }
}
