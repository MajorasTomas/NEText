using Microsoft.AspNetCore.Mvc;

namespace NEText.Api.Controllers;

// Simplest possible endpoint: proves the server is running and reachable.
[ApiController]
[Route("api/[controller]")] // [controller] = class name without "Controller" -> /api/health
public class HealthController : ControllerBase
{
    // GET /api/health
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", serverTimeUtc = DateTime.UtcNow });
    }
}
