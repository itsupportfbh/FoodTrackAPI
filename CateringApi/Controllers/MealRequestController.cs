using CateringApi.DTOs.MealPlan;

using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MealRequestController : ControllerBase
    {
        private readonly IMealRequestRepository _repository;

        public MealRequestController(IMealRequestRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("GetAllMealRequests")]
        public async Task<IActionResult> GetAllMealRequests(int companyId, int userId)
        {
            var data = await _repository.GetAllMealRequests(companyId, userId);

            return Ok(new
            {
                status = true,
                message = "Meal requests loaded successfully.",
                data
            });
        }

        [HttpGet("GetMealRequestById/{id}")]
        public async Task<IActionResult> GetMealRequestById(int id)
        {
            var data = await _repository.GetMealRequestById(id);

            if (data == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Meal request not found.",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                status = true,
                message = "Meal request loaded successfully.",
                data
            });
        }

        [HttpPost("SaveMealRequest")]
        public async Task<IActionResult> SaveMealRequest([FromBody] SaveMealRequestDto dto)
        {
            var result = await _repository.SaveMealRequest(dto);
            return Ok(result);
        }

        [HttpDelete("DeleteMealRequest/{id}")]
        public async Task<IActionResult> DeleteMealRequest(int id)
        {
            var result = await _repository.DeleteMealRequest(id);
            return Ok(result);
        }



        [HttpGet("ShowQr")]
        public async Task<IActionResult> ShowQr(int companyId, int userId)
        {
            var data = await _repository.ShowQr(companyId, userId);

            return Ok(new
            {
                status = true,
                message = "QR Code loaded successfully.",
                data
            });
        }
    }
}