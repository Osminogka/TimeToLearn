using Authentication.DAL.Models;
using Authentication.DL.Repositories;
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
        private readonly IUsersRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUsersRepository usersRepository, IConfiguration configuration)
        {
            _userRepository = usersRepository;
            _configuration = configuration;
        }

        public async Task<ResponseMessage> LoginAsync(LoginRequestModel loginModel)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _userRepository.GetByEmailAsync(loginModel.Email);
            if (user == null)
            {
                response.Message = "User doesn't exist";
                return response;
            }

            var result = await _userRepository.CheckPasswordSignInAsync(user, loginModel.Password, false);
            if (result.Succeeded)
            {
                response.Success = true;
                response.Message = await TokenGenerator(user);
                return response;
            }

            await _userRepository.SetUserRoleAsync(user, Roles.Student);

            response.Message = "Invalid password";
            return response;
        }

        public async Task<ResponseMessage> RegisterAsync(RegisterRequestModel model)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "Couldn't create user";

            var user = new AppUser
            {
                UserName = model.Name,
                SecurityStamp = Guid.NewGuid().ToString(),
                Email = model.Email
            };

            var userExist = await _userRepository.GetByEmailAsync(model.Email);
            if (userExist != null)
            {
                response.Message = "User already exist";
                return response;
            }
            var result = await _userRepository.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                response.Success = true;
                response.Message = await TokenGenerator(user);
                return response;
            }

            return response;
        }

        public async Task<ResponseMessage> BecomeATeacherAsync(AppUser user, TeacherModel model)
        {
            ResponseMessage response = new ResponseMessage();

            //var accountTypeClaim = new Claim("AccountType", Roles.Teacher);
            //var universityClaim = new Claim("University", model.UniversityName);
            //var degreeClaim = new Claim("Degree", model.Degree);

            //var claims = await _userRepository.GetAllClaimsAsync(user);
            //if (claims.ToArray().Where(claim => claim.Value == "Teacher").Count() != 0)
            //    return response;

            //await _userRepository.AddClaimAsync(user, accountTypeClaim);
            //await _userRepository.AddClaimAsync(user, universityClaim);
            //await _userRepository.AddClaimAsync(user, degreeClaim);

            response.Success = true;
            return response;
        }
        private async Task<string> TokenGenerator(AppUser user)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:key"]);
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = credentials,
                Subject = await GenerateClaims(user),
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        private async Task<ClaimsIdentity> GenerateClaims(AppUser user)
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Name, user.Email));

            var roles = await _userRepository.GetUserRolesAsync(user);

            foreach (var role in roles)
                claims.AddClaim(new Claim(ClaimTypes.Role, role));

            return claims;
        }
    }
}
