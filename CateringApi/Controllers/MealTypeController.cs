using CateringApi.DTOs.Common;
using CateringApi.DTOs.MealType;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MealTypeController : ControllerBase
    {
        private readonly IMealTypeRepository _repository;

        public MealTypeController(IMealTypeRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _repository.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<MealTypeDto>>.Ok(data));
        }
    }
}