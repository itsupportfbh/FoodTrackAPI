using CateringApi.Data;
using CateringApi.DTOs.Item;

using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class SessionController : ControllerBase
    {

        private readonly ISessionRepository _repository;

        public SessionController(ISessionRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSession()
        {
            var list = await _repository.GetAllSession();
            ResponseResult data = new ResponseResult(true, "Success", list);
            return Ok(data);
        }





        [HttpGet]
        public async Task<IActionResult> GetSessionById(int id)
        {
            var licenseObj = await _repository.GetSessionById(id);
            ResponseResult data = new ResponseResult(true, "Success", licenseObj);
            return Ok(data);
        }



        [HttpPost]
        public async Task<ActionResult> CreateSession(SessionDTO SessionDTO)
        {

            var id = await _repository.CreateSession(SessionDTO);
            ResponseResult data = new ResponseResult(true, "Session created sucessfully", id);
            return Ok(data);

        }

        [HttpPut]
        public async Task<IActionResult> UpdateSession(SessionDTO SessionDTO)
        {
            await _repository.UpdateSession(SessionDTO);
            ResponseResult data = new ResponseResult(true, "Session updated successfully.", null);
            return Ok(data);
        }



        [HttpDelete]
        public async Task<IActionResult> DeleteSession(int id)
        {
            await _repository.DeleteSession(id);
            ResponseResult data = new ResponseResult(true, "Session Deleted sucessfully", null);
            return Ok(data);
        }
    }
}
