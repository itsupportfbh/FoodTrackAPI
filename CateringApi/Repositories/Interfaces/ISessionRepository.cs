

using CateringApi.DTOs.Item;

namespace CateringApi.Repositories.Interfaces
{
    public interface ISessionRepository
    {
        Task<IEnumerable<SessionDTO>> GetAllSession();
        Task<SessionDTO> GetSessionById(long id);
        Task<int> CreateSession(SessionDTO SessionDTO);
        Task UpdateSession(SessionDTO SessionDto);
        Task DeleteSession(int id);
    }
}
