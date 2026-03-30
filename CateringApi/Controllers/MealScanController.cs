using CateringApi.DTOs.Common;
using CateringApi.DTOs.MealScan;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MealScanController : ControllerBase
    {
        private readonly IMealScanRepository _repository;

        public MealScanController(IMealScanRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("scan")]
        public async Task<IActionResult> SaveScan([FromBody] MealScanSaveDto dto)
        {
            var result = await _repository.SaveScanAsync(dto);

            if (!result.IsValid)
                return Ok(ApiResponse<MealScanResultDto>.Fail(result.Message) with { Data = result });

            return Ok(ApiResponse<MealScanResultDto>.Ok(result, result.Message));
        }
    }
}