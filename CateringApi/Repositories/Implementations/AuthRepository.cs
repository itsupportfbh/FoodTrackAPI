using CateringApi.Data;
using CateringApi.DTOs.Auth;
using CateringApi.DTOs.User;
using CateringApi.Repositories.Interfaces;
using CateringApi.Services;
using Dapper;
using System.Data;

namespace CateringApi.Repositories.Implementations
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DapperContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthRepository(DapperContext context, IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
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

        public async Task<ResponseResult> ChangePasswordAsync(ChangePasswordDto dto)
        {
            using var con = _context.CreateConnection();
            if (con.State != ConnectionState.Open)
                con.Open();

            using var tx = con.BeginTransaction();

            try
            {
                if (dto.UserId <= 0 || dto.CompanyId <= 0)
                    return new ResponseResult(false, "Invalid user details", null);

                if (string.IsNullOrWhiteSpace(dto.Email))
                    return new ResponseResult(false, "Email is required", null);

                if (string.IsNullOrWhiteSpace(dto.OldPassword))
                    return new ResponseResult(false, "Old password is required", null);

                if (string.IsNullOrWhiteSpace(dto.NewPassword))
                    return new ResponseResult(false, "New password is required", null);

                if (dto.NewPassword != dto.ConfirmPassword)
                    return new ResponseResult(false, "Passwords do not match", null);

                if (dto.NewPassword == dto.OldPassword)
                    return new ResponseResult(false, "New password must be different", null);

                const string sql = @"
SELECT TOP 1 Id, CompanyId, Email, PasswordHash, IsActive
FROM dbo.UserMaster
WHERE Id = @UserId
  AND CompanyId = @CompanyId
  AND Email = @Email
  AND IsActive = 1";

                var user = await con.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    dto.UserId,
                    dto.CompanyId,
                    dto.Email
                }, tx);

                if (user == null)
                    return new ResponseResult(false, "User not found", null);

                string hash = user.PasswordHash ?? "";

                if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, hash))
                    return new ResponseResult(false, "Old password incorrect", null);

                string newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

                const string updateSql = @"
UPDATE dbo.UserMaster
SET PasswordHash = @PasswordHash,
    UpdatedBy = @UpdatedBy,
    UpdatedDate = GETDATE()
WHERE Id = @UserId AND CompanyId = @CompanyId";

                var rows = await con.ExecuteAsync(updateSql, new
                {
                    PasswordHash = newHash,
                    UpdatedBy = dto.UserId,
                    dto.UserId,
                    dto.CompanyId
                }, tx);

                if (rows <= 0)
                {
                    tx.Rollback();
                    return new ResponseResult(false, "Update failed", null);
                }

                tx.Commit();

                return new ResponseResult(true, "Password changed successfully", null);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return new ResponseResult(false, ex.Message, null);
            }
        }

        public async Task<ResponseResult> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return new ResponseResult(false, "Email is required", null);

                dto.Mode = string.IsNullOrWhiteSpace(dto.Mode) ? "password" : dto.Mode.Trim().ToLower();

                if (dto.Mode != "username" && dto.Mode != "password")
                    return new ResponseResult(false, "Invalid mode", null);

                using var con = _context.CreateConnection();

                const string sql = @"
SELECT TOP 1 Id, CompanyId, Username, Email, IsActive
FROM dbo.UserMaster
WHERE Email = @Email";

                var user = await con.QueryFirstOrDefaultAsync<dynamic>(sql, new { dto.Email });

                if (user == null)
                    return new ResponseResult(false, "No user found with this email", null);

                if (!(bool)user.IsActive)
                    return new ResponseResult(false, "User is inactive", null);

                if (dto.Mode == "username")
                {
                    string subject = "Your Username - UnityWorks ERP";
                    string body = $@"
                        <p>Hello,</p>
                        <p>Your username is: <b>{user.Username}</b></p>
                        <p>Please use this username to login.</p>
                        <p>Thanks,<br/>UnityWorks ERP Team</p>";

                    await _emailService.SendEmailAsync(dto.Email, subject, body);

                    return new ResponseResult(true, "Username sent to your email", null);
                }
                else
                {
                    string token = Guid.NewGuid().ToString("N");

                    const string deleteOldTokenSql = @"
DELETE FROM dbo.PasswordResetTokens
WHERE Email = @Email";

                    await con.ExecuteAsync(deleteOldTokenSql, new { dto.Email });

                    const string insertTokenSql = @"
INSERT INTO dbo.PasswordResetTokens
(
    Email,
    Token,
    ExpiryDate,
    IsUsed,
    CreatedDate
)
VALUES
(
    @Email,
    @Token,
    DATEADD(MINUTE, 30, GETDATE()),
    0,
    GETDATE()
)";

                    await con.ExecuteAsync(insertTokenSql, new
                    {
                        Email = dto.Email,
                        Token = token
                    });

                    string frontendBaseUrl = _configuration["AppSettings:FrontendBaseUrl"] ?? "https://qr.fbh.com.sg";
                    string resetLink = $"{frontendBaseUrl}/pages/authentication/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={token}";

                    string subject = "Reset Password - UnityWorks ERP";
                    string body = $@"
                        <p>Hello,</p>
                        <p>Click the link below to reset your password:</p>
                        <p><a href='{resetLink}'>Reset Password</a></p>
                        <p>This link will expire in 30 minutes.</p>
                        <p>Thanks,<br/>UnityWorks ERP Team</p>";

                    await _emailService.SendEmailAsync(dto.Email, subject, body);

                    return new ResponseResult(true, "Reset link sent to your email", null);
                }
            }
            catch (Exception ex)
            {
                return new ResponseResult(false, ex.Message, null);
            }
        }

        public async Task<ResponseResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return new ResponseResult(false, "Email is required", null);

                if (string.IsNullOrWhiteSpace(dto.Token))
                    return new ResponseResult(false, "Token is required", null);

                if (string.IsNullOrWhiteSpace(dto.NewPassword))
                    return new ResponseResult(false, "New password is required", null);

                if (dto.NewPassword != dto.ConfirmPassword)
                    return new ResponseResult(false, "Passwords do not match", null);

                using var con = _context.CreateConnection();
                if (con.State != ConnectionState.Open)
                    con.Open();

                using var tx = con.BeginTransaction();

                const string tokenSql = @"
SELECT TOP 1 *
FROM dbo.PasswordResetTokens
WHERE Email = @Email
  AND Token = @Token
  AND IsUsed = 0
  AND ExpiryDate >= GETDATE()
ORDER BY CreatedDate DESC";

                var tokenRow = await con.QueryFirstOrDefaultAsync<dynamic>(tokenSql, new
                {
                    dto.Email,
                    dto.Token
                }, tx);

                if (tokenRow == null)
                {
                    tx.Rollback();
                    return new ResponseResult(false, "Invalid or expired reset link", null);
                }

                const string userSql = @"
SELECT TOP 1 Id, CompanyId, PasswordHash
FROM dbo.UserMaster
WHERE Email = @Email AND IsActive = 1";

                var user = await con.QueryFirstOrDefaultAsync<dynamic>(userSql, new
                {
                    dto.Email
                }, tx);

                if (user == null)
                {
                    tx.Rollback();
                    return new ResponseResult(false, "User not found", null);
                }

                string newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

                const string updateUserSql = @"
UPDATE dbo.UserMaster
SET PasswordHash = @PasswordHash,
    UpdatedBy = Id,
    UpdatedDate = GETDATE()
WHERE Email = @Email";

                await con.ExecuteAsync(updateUserSql, new
                {
                    PasswordHash = newHash,
                    dto.Email
                }, tx);

                const string useTokenSql = @"
UPDATE dbo.PasswordResetTokens
SET IsUsed = 1
WHERE Email = @Email
  AND Token = @Token";

                await con.ExecuteAsync(useTokenSql, new
                {
                    dto.Email,
                    dto.Token
                }, tx);

                tx.Commit();

                return new ResponseResult(true, "Password reset successfully", null);
            }
            catch (Exception ex)
            {
                return new ResponseResult(false, ex.Message, null);
            }
        }

    }
}