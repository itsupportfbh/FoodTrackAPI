using CateringApi.DTOs.Auth;
using CateringApi.Repositories.Interfaces;
using CateringApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtService _jwtService;

        public AuthController(IAuthRepository authRepository, IJwtService jwtService)
        {
            _authRepository = authRepository;
            _jwtService = jwtService;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email and Password are required"
                });
            }

            var user = await _authRepository.GetUserByEmailAsync(request.Email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid email or password"
                });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User is inactive"
                });
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid email or password"
                });
            }

            var token = _jwtService.GenerateToken(
                user.Id,
                user.Username,
                user.Email,
                user.RoleId,
                user.CompanyId
            );

            var response = new LoginResponseDto
            {
                Id = user.Id,
                CompanyId = user.CompanyId,
                RoleId = user.RoleId,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                Token = token
            };

            return Ok(new
            {
                success = true,
                message = "Login successful",
                data = response
            });
        }
    }
}