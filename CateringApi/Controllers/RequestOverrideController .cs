using CateringApi.DTOs.RequestOverride;
using CateringApi.Repositories.Implementations;
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
     int requestHeaderId,
     DateTime fromDate,
     DateTime toDate,
     int? requestOverrideId   // 👈 ADD THIS
 )
        {
            var data = await _repository.GetScreenDataAsync(requestHeaderId, fromDate, toDate, requestOverrideId);

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
            if (dto == null)
            {
                return Ok(new SaveRequestOverrideResultDto
                {
                    IsSuccess = false,
                    Message = "Invalid request payload.",
                    MessageType = "error"
                });
            }

            if (dto.RequestHeaderId <= 0)
            {
                return Ok(new SaveRequestOverrideResultDto
                {
                    IsSuccess = false,
                    Message = "Request is required.",
                    MessageType = "warning"
                });
            }

            if (dto.FromDate.Date > dto.ToDate.Date)
            {
                return Ok(new SaveRequestOverrideResultDto
                {
                    IsSuccess = false,
                    Message = "From date cannot be greater than To date.",
                    MessageType = "warning"
                });
            }

            if (dto.Lines == null || dto.Lines.Count == 0)
            {
                return Ok(new SaveRequestOverrideResultDto
                {
                    IsSuccess = false,
                    Message = "Override details are required.",
                    MessageType = "warning"
                });
            }

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