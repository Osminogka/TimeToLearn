using Microsoft.AspNetCore.Mvc;
using Users.DAL.Dtos;

namespace Users.API.Controllers
{
    [Route("api/[controller]/")]
    public class UniversityController : BaseController
    {
        private readonly ILogger<UniversityController> _logger;

        public UniversityController(ILogger<UniversityController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> CreateUniversity(CreateUniversityDto model)
        {
            return Ok();   
        }
    }
}
