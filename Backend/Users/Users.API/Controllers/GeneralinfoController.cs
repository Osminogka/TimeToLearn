using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Core.DL.Services;


namespace Core.API.Controllers
{
    [ApiController]
    [Route("api/u/general")]
    public class GeneralinfoController : ControllerBase
    {
        private readonly IGeneralInfoService _generalInfoService;
        private readonly ILogger<BaseUserController> _logger;

        public GeneralinfoController(IGeneralInfoService generalInfoService, ILogger<BaseUserController> logger)
        {
            _generalInfoService = generalInfoService;
            _logger = logger;
        }

        [HttpGet("{userEmail}")]
        public async Task<IActionResult> GetUserRoleAsync(string userEmail)
        {
            try
            {
                var result = await _generalInfoService.GetUserRoleAsync(userEmail);
                if (!result.Success)
                    return BadRequest(result.Message);
                return Ok(result);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }

        protected string getUserEmail()
        {
            return HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;
        }

        protected IActionResult HandleException(Exception ex)
        {
            if (ex is ArgumentNullException)
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the request.");
            else if (ex is InvalidOperationException)
                return StatusCode(StatusCodes.Status500InternalServerError, "More than one element satisfies the condition in SingleOrDefault.");
            else if (ex is DbUpdateException)
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the database.");
            else
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }
}
