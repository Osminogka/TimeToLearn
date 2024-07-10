using System.Security.Claims;
using Authentication.DAL.Models;
using Authentication.DL.Models;
using Microsoft.AspNetCore.Identity;


namespace Authentication.DL.Services
{
    public interface IUsersService
    {
        IQueryable<UserModel> Get();
        UserModel GetByEmail(string email);
        Task<IdentityResult> Create(UserModel user, string password);
        Task<IdentityResult> Delete(UserModel user);
        Task<IdentityResult> Update(UserModel user);
        Task<IdentityResult> ValidatePassword(UserModel user, string password);
        Task<IdentityResult> ValidateUser(UserModel user);
        string HashPassword(UserModel user, string password);
        Task SignOutAsync();
        Task<SignInResult> CheckPasswordSignInAsync(UserModel user, string password, bool lockoutOnFailure);
        Task<IdentityResult> AddClaimAsync(UserModel user, Claim claim);
        Task<IList<string>> GetUserRolesAsync(UserModel user);
        Task<IdentityResult> SetUserRoleAsync(UserModel user, string role);
    }
}
