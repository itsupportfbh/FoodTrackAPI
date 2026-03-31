using CateringApi.DTOs.User;

namespace CateringApi.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<UserLoginDto?> GetUserByEmailAsync(string email);
    }
}
