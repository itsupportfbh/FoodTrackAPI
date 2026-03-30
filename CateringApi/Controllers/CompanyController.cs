using CateringApi.DTOs.Common;
using CateringApi.DTOs.Company;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyRepository _repository;

        public CompanyController(ICompanyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _repository.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<CompanyDto>>.Ok(data));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _repository.GetByIdAsync(id);
            if (data == null)
                return NotFound(ApiResponse<CompanyDto>.Fail("Company not found"));

            return Ok(ApiResponse<CompanyDto>.Ok(data));
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] CompanySaveDto dto)
        {
            var id = await _repository.SaveAsync(dto);
            return Ok(ApiResponse<int>.Ok(id, "Company saved successfully"));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int? userId)
        {
            var ok = await _repository.DeleteAsync(id, userId);
            if (!ok)
                return BadRequest(ApiResponse<string>.Fail("Unable to delete company"));

            return Ok(ApiResponse<string>.Ok(null, "Company deactivated successfully"));
        }
    }
}