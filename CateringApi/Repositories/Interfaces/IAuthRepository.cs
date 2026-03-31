using CateringApi.Data;
using CateringApi.DTOs.Auth;
using CateringApi.DTOs.User;

namespace CateringApi.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<UserLoginDto?> GetUserByEmailAsync(string email);
        Task<ResponseResult> ChangePasswordAsync(ChangePasswordDto dto);
        Task<ResponseResult> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<ResponseResult> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
