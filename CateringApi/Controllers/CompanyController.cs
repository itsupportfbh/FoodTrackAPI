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
            return Ok(ApiResponse<IEnumerable<CompanyMaster>>.Ok(data));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _repository.GetByIdAsync(id);
            if (data == null)
                return NotFound(ApiResponse<CompanySaveDto>.Fail("Company not found"));

            return Ok(ApiResponse<CompanySaveDto>.Ok(data));
        }
        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] CompanySaveDto dto)
        {
            try
            {
                var id = await _repository.SaveAsync(dto);
                return Ok(ApiResponse<int>.Ok(id, "Company saved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
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