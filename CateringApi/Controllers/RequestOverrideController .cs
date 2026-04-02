using CateringApi.DTOs.RequestOverride;
using CateringApi.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RequestOverrideController : ControllerBase
{
    private readonly IRequestOverrideRepository _service;

    public RequestOverrideController(IRequestOverrideRepository service)
    {
        _service = service;
    }

    [HttpGet("screen")]
    public async Task<IActionResult> GetScreen([FromQuery] int requestHeaderId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        var data = await _service.GetScreenDataAsync(requestHeaderId, fromDate, toDate);

        return Ok(new
        {
            isSuccess = true,
            message = "Success",
            data
        });
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] SaveRequestOverrideDto dto)
    {
        var id = await _service.SaveAsync(dto);

        return Ok(new
        {
            isSuccess = true,
            message = "Override saved successfully",
            data = new { id }
        });
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList([FromQuery] int requestHeaderId)
    {
        var data = await _service.GetOverrideListAsync(requestHeaderId);

        return Ok(new
        {
            isSuccess = true,
            message = "Success",
            data
        });
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int updatedBy = 0)
    {
        await _service.DeleteAsync(id, updatedBy);

        return Ok(new
        {
            isSuccess = true,
            message = "Override deleted successfully"
        });
    }
}