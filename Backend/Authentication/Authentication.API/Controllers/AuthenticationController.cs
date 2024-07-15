using Authentication.API.AsyncDataService;
using Authentication.DAL.Dtos;
using Authentication.DAL.Models;
using Authentication.DL.Repositories;
using Authentication.DL.Services;
using AutoMapper;
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
        private readonly IMessageBusClient _messageBusClient;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(IAuthService authService, IUsersRepository usersRepository, IMessageBusClient messageBusClient, IMapper mapper ,ILogger<AuthenticationController> logger)

        {
            _authService = authService;
            _userRepository = usersRepository;
            _messageBusClient = messageBusClient;
            _mapper = mapper;
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
                var userPublishedDto = _mapper.Map<BaseUserPublishDto>(result.User);
                userPublishedDto.Event = "BaseUser_Published";
                _messageBusClient.PublishNewUser(userPublishedDto);

                return Ok(result);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return HandleException(ex);
            }
        }
    }
}
