using Microsoft.AspNetCore.Mvc;
using Users.DAL.SideModels;
using Users.DL.Services;

namespace Users.API.Controllers
{
    [Route("api/user/")]
    public class BaseUserController : BaseController
    {
        private readonly IBaseUserService _baseUserService;
        private readonly ILogger<BaseUserController> _logger;

        public BaseUserController(IBaseUserService baseUserService, ILogger<BaseUserController> logger)
        {
            _baseUserService = baseUserService;
            _logger = logger;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetUsersAsync()
        {
            try
            {
                var result = await _baseUserService.GetUsersAsync();
                if (!result.Success)
                    return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetBaseUserAsync(string name)
        {
            try
            {
                var result = await _baseUserService.GetBaseUserAsync(name);
                if (!result.Success)
                    return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateUserInfoAsync(UpdateUserInfoModel model)
        {
            try
            {
                var result = await _baseUserService.UpdateUserInfoAsync(model, getUserEmail());
                if (!result.Success)
                    return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }

        [HttpGet("invites")]
        public async Task<IActionResult> GetInvitesAsync()
        {
            try
            {
                var result = await _baseUserService.GetInvitesAsync(getUserEmail());
                if (!result.Success)
                    return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }

        [HttpPut("invites/{universityName}/accept")]
        public async Task<IActionResult> AcceptInviteAsync(string universityName)
        {
            try
            {
                var result = await _baseUserService.AcceptInviteAsync(universityName, getUserEmail());
                if (!result.Success)
                    return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }

        [HttpDelete("invites/{universityName}/reject")]
        public async Task<IActionResult> RejectInviteAsync(string universityName)
        {
            try
            {
                var result = await _baseUserService.RejectInviteAsync(universityName, getUserEmail());
                if (!result.Success)
                    return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }
    }
}
