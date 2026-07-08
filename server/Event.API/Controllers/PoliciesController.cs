using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Event.Contracts.IServices;
using Event.Models.DTOs;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/policies")]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PoliciesController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        [HttpGet("{type}")]
        public async Task<IActionResult> GetPolicyByType(string type)
        {
            var response = await _policyService.GetPolicyByTypeAsync(type);
            if (response == null)
            {
                return NotFound(new { Message = $"Active policy of type '{type}' not found." });
            }

            return Ok(response);
        }
    }
}
