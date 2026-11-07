using Microsoft.AspNetCore.Mvc;

namespace NEText.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", serverTimeUtc = DateTime.UtcNow });
    }
}
