using CateringApi.Data;
using CateringApi.DTOs;
using CateringApi.DTOs.User;

using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            ResponseResult data = new ResponseResult(true, "Success", list);
            return Ok(data);
        }


        [HttpGet("GetUserMasterById")]
        public async Task<IActionResult> GetUserMasterById(int id)
        {
            var licenseObj = await _userMasterRepository.GetByIdAsync(id);
            ResponseResult data = new ResponseResult(true, "Success", licenseObj);
            return Ok(data);
        }



        [HttpPost("CreateUserMaster")]
        public async Task<ActionResult> CreateUserMaster(CreateUserMasterDto userMaster)
        {

            var id = await _userMasterRepository.CreateAsync(userMaster);
            ResponseResult data = new ResponseResult(true, "UserMaster created sucessfully", id);
            return Ok(data);

        }

        [HttpPut("UpdateUserMaster")]
        public async Task<IActionResult> UpdateUserMaster(UserMaster userMaster)
        {
            await _userMasterRepository.UpdateAsync(userMaster);
            ResponseResult data = new ResponseResult(true, "UserMaster updated successfully.", null);
            return Ok(data);
        }



        [HttpDelete("DeleteUserMaster")]
        public async Task<IActionResult> DeleteUserMaster(int id, int updatedBy)
        {
            await _userMasterRepository.DeleteAsync(id, updatedBy);
            ResponseResult data = new ResponseResult(true, "UserMaster Deleted sucessfully", null);
            return Ok(data);
        }
    }
}
