using Authentication.DAL.Models;
using Authentication.DL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.API.Controllers
{
    [Route("api/[controller]/")]
    public class AuthenticationController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(IAuthService authService ,ILogger<AuthenticationController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody]LoginRequestModel loginModel)
        {
            return Ok();
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody]RegisterRequestModel model)
        {
            return Ok();
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost("teacher")]
        public async Task<IActionResult> BecomeATeacherAsync([FromBody] TeacherModel model)
        {
            ResponseMessage response = new ResponseMessage();

            return Ok(response);
        }
    }
}
