using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new { status = "Healthy", timestamp = System.DateTime.UtcNow });
        }
    }
}
