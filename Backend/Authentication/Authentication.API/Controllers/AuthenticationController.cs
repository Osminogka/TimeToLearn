using Authentication.DAL.Models;
using Authentication.DL.Repositories;
using Authentication.DL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Authentication.API.Controllers
{
    [Route("api/[controller]/")]
    public class AuthenticationController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IUsersRepository _userRepository;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(IAuthService authService, IUsersRepository usersRepository,ILogger<AuthenticationController> logger)

        {
            _authService = authService;
            _userRepository = usersRepository;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody]LoginRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid credentials");
                var result = await _authService.LoginAsync(model);
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

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody]RegisterRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest("Invalid credentials");
                var result = await _authService.RegisterAsync(model);
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

        [HttpPost("teacher")]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> BecomeATeacherAsync([FromBody] TeacherModel model)
        {
            ResponseMessage response = new ResponseMessage();
            try
            {
                var user = await _userRepository.GetByEmailAsync(getUserEmail());
                var result = await _authService.BecomeATeacherAsync(user, model);
                if (!result.Success)
                    return Ok(response);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }

            response.Success = true;
            response.Message = "Request is sent";
            return Ok(response);
        }
    }
}
