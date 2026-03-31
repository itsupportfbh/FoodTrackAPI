using CateringApi.Data;
using CateringApi.DTOs.User;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DapperContext _context;

        public AuthRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<UserLoginDto?> GetUserByEmailAsync(string email)
        {
            const string query = @"
                SELECT TOP 1
                    Id,
                    CompanyId,
                    RoleId,
                    Username,
                    Email,
                    PasswordHash,
                    IsActive
                FROM dbo.UserMaster
                WHERE Email = @Email
            ";

            using var connection = _context.CreateConnection();

            return await connection.QueryFirstOrDefaultAsync<UserLoginDto>(
                query,
                new { Email = email }
            );
        }
    }
}