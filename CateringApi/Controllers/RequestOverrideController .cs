using CateringApi.DTOs.RequestOverride;
using CateringApi.Repositories.Implementations;
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
        Console.WriteLine("CONTROLLER SAVE HIT: " + DateTime.Now.ToString("HH:mm:ss.fff"));

        var id = await _service.SaveAsync(dto);

        Console.WriteLine("CONTROLLER SAVE SUCCESS ID: " + id);

        return Ok(new
        {
            isSuccess = true,
            id = id,
            message = "Override saved successfully"
        });
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetOverrideList([FromQuery] int companyId = 0)
    {
        var result = await _service.GetOverrideList(companyId);
        return Ok(result);
    }

    [HttpGet("lines/{requestOverrideId}")]
    public async Task<IActionResult> GetOverrideLines(int requestOverrideId)
    {
        var result = await _service.GetOverrideLines(requestOverrideId);
        return Ok(result);
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