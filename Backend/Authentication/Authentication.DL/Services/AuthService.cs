using Authentication.DAL.Models;
using Authentication.DL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Authentication.DL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsersService _userService;
        private readonly IConfiguration _configuration;

        public AuthService(IUsersService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
        }

        public async Task<ResponseMessage> LoginAsync(LoginRequestModel loginModel)
        {
            ResponseMessage response = new ResponseMessage();

            var user = _userService.GetByEmail(loginModel.Email);
            if (user == null)
            {
                response.Message = "User doesn't exist";
                return response;
            }

            var result = await _userService.CheckPasswordSignInAsync(user, loginModel.Password, false);
            if (result.Succeeded)
            {
                response.Success = true;
                response.Message = await TokenGenerator(user);
                return response;
            }

            await _userService.SetUserRoleAsync(user, Roles.Student);

            response.Message = "Invalid password";
            return response;
        }

        public async Task<ResponseMessage> RegisterAsync(RegisterRequestModel model)
        {
            ResponseMessage response = new ResponseMessage();

            var user = new UserModel
            {
                UserName = model.Name,
                Email = model.Email
            };

            var result = await _userService.Create(user, model.Password);

            if (result.Succeeded)
            {
                response.Success = true;
                response.Message = await TokenGenerator(user);
                return response;
            }

            response.Message = "Couldn't create user";
            return response;
        }

        public async Task<ResponseMessage> BecomeATeacherAsync([FromBody] TeacherModel model)
        {
            ResponseMessage response = new ResponseMessage();

            return response;
        }
        private async Task<string> TokenGenerator(UserModel user)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:key"]);
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = await GenerateClaims(user),
                SigningCredentials = credentials,
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        private async Task<ClaimsIdentity> GenerateClaims(UserModel user)
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Name, user.Email));

            var roles = await _userService.GetUserRolesAsync(user);

            foreach (var role in roles)
                claims.AddClaim(new Claim(ClaimTypes.Role, role));

            return claims;
        }
    }
}
