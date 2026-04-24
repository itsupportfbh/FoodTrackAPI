using CateringApi.DTOs.RequestOverride;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CateringApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RequestOverrideController : ControllerBase
    {
        private readonly IRequestOverrideRepository _repository;

        public RequestOverrideController(IRequestOverrideRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("GetScreenData")]
        public async Task<IActionResult> GetScreenData(
            [FromQuery] int requestHeaderId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            var data = await _repository.GetScreenDataAsync(requestHeaderId, fromDate, toDate);

            if (data == null)
            {
                return Ok(new
                {
                    isSuccess = false,
                    message = "Request override data not found.",
                    messageType = "error",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                isSuccess = true,
                message = "Data loaded successfully.",
                messageType = "success",
                data
            });
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromBody] SaveRequestOverrideDto dto)
        {
            var result = await _repository.SaveAsync(dto);
            return Ok(result);
        }

        [HttpGet("GetOverrideList")]
        public async Task<IActionResult> GetOverrideList([FromQuery] int companyId)
        {
            var data = await _repository.GetOverrideList(companyId);

            return Ok(new
            {
                isSuccess = true,
                message = "Data loaded successfully.",
                data
            });
        }

        [HttpGet("GetOverrideLines")]
        public async Task<IActionResult> GetOverrideLines([FromQuery] int requestOverrideId)
        {
            var data = await _repository.GetOverrideLines(requestOverrideId);

            return Ok(new
            {
                isSuccess = true,
                message = "Data loaded successfully.",
                data
            });
        }

        [HttpDelete("Delete/{id}/{updatedBy}")]
        public async Task<IActionResult> Delete(int id, int updatedBy)
        {
            await _repository.DeleteAsync(id, updatedBy);

            return Ok(new
            {
                isSuccess = true,
                message = "Override deleted successfully.",
                messageType = "success"
            });
        }
    }
}