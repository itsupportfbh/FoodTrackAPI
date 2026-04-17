using CateringApi.DTOs.Common;
using CateringApi.DTOs.Employee;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeRepository _repository;

        public EmployeeController(IEmployeeRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _repository.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<EmployeeDto>>.Ok(data));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _repository.GetByIdAsync(id);
            if (data == null)
                return NotFound(ApiResponse<EmployeeDto>.Fail("Employee not found"));

            return Ok(ApiResponse<EmployeeDto>.Ok(data));
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] EmployeeSaveDto dto)
        {
            var id = await _repository.SaveAsync(dto);
            return Ok(ApiResponse<int>.Ok(id, "Employee saved successfully"));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int? userId)
        {
            var ok = await _repository.DeleteAsync(id, userId);
            if (!ok)
                return BadRequest(ApiResponse<string>.Fail("Unable to delete employee"));

            return Ok(ApiResponse<string>.Ok(null, "Employee deactivated successfully"));
        }
    }
}