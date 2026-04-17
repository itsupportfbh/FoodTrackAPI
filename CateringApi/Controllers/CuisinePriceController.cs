using CateringApi.DTOs;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuisinePriceController : ControllerBase
    {
        private readonly ICuisinePriceRepository _repository;

        public CuisinePriceController(ICuisinePriceRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("all-cuisines-with-rates")]
        public async Task<IActionResult> GetAllCuisinesWithRates([FromQuery] int companyId, [FromQuery] int sessionId)
        {
            try
            {
                var data = await _repository.GetAllCuisinesWithRatesAsync(companyId, sessionId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("save-bulk")]
        public async Task<IActionResult> SaveSessionRate([FromBody] SessionRateSaveRequest request)
        {
            var result = await _repository.SaveSessionRateAsync(request);

            return Ok(new
            {
                success = result,
                message = "Session price saved successfully"
            });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int companyId, [FromQuery] int sessionId, [FromQuery] int cuisineId)
        {
            try
            {
                var data = await _repository.GetCuisinePriceHistoryAsync(companyId, sessionId, cuisineId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        [HttpGet("GetPriceList")]
        public async Task<IActionResult> GetPriceList()
        {
            var result = await _repository.GetPriceList();
            return Ok(result);
        }
    }
}