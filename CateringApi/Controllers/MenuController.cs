using CateringApi.DTOs;
using CateringApi.DTOs.Menu;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly IMenuRepository _repository;

        public MenuController(IMenuRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveMenuUpload([FromBody] SaveMenuUploadRequestDto request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request." });

            if (request.MenuMonth <= 0 || request.MenuMonth > 12)
                return BadRequest(new { success = false, message = "Invalid month." });

            if (request.MenuYear <= 0)
                return BadRequest(new { success = false, message = "Invalid year." });

            if (request.Rows == null || !request.Rows.Any())
                return BadRequest(new { success = false, message = "No rows found." });

            var result = await _repository.SaveMenuUploadAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetMenuByMonthYear([FromQuery] int month, [FromQuery] int year)
        {
            if (month <= 0 || month > 12)
                return BadRequest(new { success = false, message = "Invalid month." });

            if (year <= 0)
                return BadRequest(new { success = false, message = "Invalid year." });

            var result = await _repository.GetMenuByMonthYearAsync(month, year);
            return Ok(result);
        }
    }
}