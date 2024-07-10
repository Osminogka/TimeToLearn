using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authentication.API.Infrastructure;
using Authentication.API.Models;
using Authentication.DL.Models;
using Authentication.DL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace Authentication.API.Controllers
{
    [Route("api/[controller]/")]
    public class AuthenticationController : BaseController
    {
        private readonly IUsersService _userService;
        private readonly IClaimManager _claimManager;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IConfiguration _configuration;

        public AuthenticationController(IUsersService userService, IClaimManager claimManager,IConfiguration configuration ,ILogger<AuthenticationController> logger)
        {
            _userService = userService;
            _claimManager = claimManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody]LoginRequestModel loginModel)
        {
            ResponseMessage response = new ResponseMessage();
            UserModel user = new UserModel();
            Microsoft.AspNetCore.Identity.SignInResult result = new Microsoft.AspNetCore.Identity.SignInResult();

            try
            {
                user = _userService.GetByEmail(loginModel.Email);
                if (user == null)
                {
                    response.Message = "User doesn't exist";
                    return Ok(response);
                }

                result = await _userService.CheckPasswordSignInAsync(user, loginModel.Password, false);
                if (result.Succeeded)
                {
                    response.Success = true;
                    response.Message = TokenGenerator(user);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            response.Message = "Invalid password";
            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody]RegisterRequestModel model)
        {
            ResponseMessage response = new ResponseMessage();

            if (!ModelState.IsValid)
            {
                response.Message = "Invalid parameters";
                return Ok(response);
            }

            var user = new UserModel
            {
                UserName = model.Name,
                Email = model.Email
            };

            IdentityResult result = new IdentityResult();
            try
            {
                result = await _userService.Create(user, model.Password);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            if (result.Succeeded)
            {
                response.Success = true;
                response.Message = TokenGenerator(user);
                return Ok(response);
            }

            response.Message = "Couldn't create user";
            return Ok(response);
            return Ok();
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost("teacher")]
        public async Task<IActionResult> BecomeATeacherAsync([FromBody] TeacherModel model)
        {
            ResponseMessage response = new ResponseMessage();

            return Ok(response);
        }
        private string TokenGenerator(UserModel user)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:key"]);
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = GenerateClaims(user),
                SigningCredentials = credentials,
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }
        
        private static ClaimsIdentity GenerateClaims(UserModel user)
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Name, user.Email));

            // foreach (var role in user.)
            //     claims.AddClaim(new Claim(ClaimTypes.Role, role));

            return claims;
        }
    }
}
