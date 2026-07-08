using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Event.Contracts.IServices;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/regions")]
    public class RegionController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IEventService _eventService;

        public RegionController(IAdminService adminService, IEventService eventService)
        {
            _adminService = adminService;
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRegions()
        {
            try
            {
                var regions = await _adminService.GetAllRegionsAsync();
                return Ok(regions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularRegions([FromQuery] int? rankNumber)
        {
            try
            {
                var regions = await _eventService.GetPopularRegionsAsync(rankNumber);
                return Ok(regions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
