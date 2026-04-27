using CateringApi.Data;
using CateringApi.DTOs;
using CateringApi.DTOs.User;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserMasterController : ControllerBase
    {
        private readonly IUserMasterRepository _userMasterRepository;

        public UserMasterController(IUserMasterRepository userMasterRepository)
        {
            _userMasterRepository = userMasterRepository;
        }

        [HttpGet("GetAllUserMaster")]
        public async Task<IActionResult> GetAllUserMaster(
            [FromQuery] long userId,
            [FromQuery] int roleId,
            [FromQuery] int companyId)
        {
            var list = await _userMasterRepository.GetAllAsync(userId, roleId, companyId);
            return Ok(new ResponseResult(true, "Success", list));
        }

        [HttpGet("GetUserMasterById")]
        public async Task<IActionResult> GetUserMasterById(int id)
        {
            var obj = await _userMasterRepository.GetByIdAsync(id);
            return Ok(new ResponseResult(true, "Success", obj));
        }

        [HttpPost("CreateUserMaster")]
        public async Task<IActionResult> CreateUserMaster([FromBody] CreateUserMasterDto userMaster)
        {
            var id = await _userMasterRepository.CreateAsync(userMaster);
            return Ok(new ResponseResult(true, "User created successfully", id));
        }

        [HttpPut("UpdateUserMaster")]
        public async Task<IActionResult> UpdateUserMaster([FromBody] UserMaster1 userMaster)
        {
            await _userMasterRepository.UpdateAsync(userMaster);
            return Ok(new ResponseResult(true, "User updated successfully", null));
        }

        [HttpDelete("DeleteUserMaster")]
        public async Task<IActionResult> DeleteUserMaster(int id, int updatedBy)
        {
            await _userMasterRepository.DeleteAsync(id, updatedBy);
            return Ok(new ResponseResult(true, "User deleted successfully", null));
        }

        [HttpGet("GetRoles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _userMasterRepository.GetRoles();
            return Ok(new ResponseResult(true, "Success", roles));
        }

        [HttpGet("DownloadUserTemplate")]
        public async Task<IActionResult> DownloadUserTemplate()
        {
            var fileBytes = await _userMasterRepository.DownloadUserTemplateAsync();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "User_Master_Template.xlsx"
            );
        }

        [HttpPost("BulkUploadUsers")]
        public async Task<IActionResult> BulkUploadUsers(
     IFormFile file,
     [FromForm] int updatedBy,
     [FromForm] int companyId)
        {
            var result = await _userMasterRepository.BulkUploadUsersAsync(file, updatedBy, companyId);
            return Ok(new ResponseResult(true, result, null));
        }


        [HttpGet("GetAllCuisine")]
        public async Task<IActionResult> GetAllCuisine(int companyId)
        {
            var obj = await _userMasterRepository.GetAllCuisine(companyId);
            return Ok(new ResponseResult(true, "Success", obj));
        }
    }
}