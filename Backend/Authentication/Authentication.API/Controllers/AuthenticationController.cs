using Authentication.API.Models;
using Authentication.DL.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/")]
    public class AuthenticationController : Controller
    {
        private readonly IUsersService _userService;

        public AuthenticationController(IUsersService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody]LoginRequestModel loginModel)
        {

            return Ok();
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody]RegisterRequestModel registerModel)
        {


            return Ok();
        }
    }
}
