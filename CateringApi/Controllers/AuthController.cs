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


        //[HttpPost]
        //public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        //{
        //    var result = await _authRepository.ChangePasswordAsync(dto);

        //    if (!result.isSuccess)
        //        return BadRequest(result);

        //    return Ok(result);
        //}

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authRepository.ForgotPasswordAsync(dto);

            if (!result.isSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authRepository.ResetPasswordAsync(dto);

            if (!result.isSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var result = await _authRepository.ChangePasswordAsync(dto);

            if (!result.isSuccess)
                return BadRequest(result);

            return Ok(result);
        }
    }
}