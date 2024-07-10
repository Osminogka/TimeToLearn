using System.Security.Claims;
using Authentication.DAL.Models;
using Microsoft.AspNetCore.Identity;

namespace Authentication.DL.Repositories
{
    public interface IUsersRepository
    {
        IQueryable<AppUser> Get();
        Task<AppUser?> GetByEmailAsync(string email);
        Task<IdentityResult> CreateAsync(AppUser user, string password);
        Task<IdentityResult> DeleteAsync(AppUser user);
        Task<IdentityResult> UpdateAsync(AppUser user);
        UserManager<AppUser> GetUserManager();
        Task<SignInResult> CheckPasswordSignInAsync(AppUser user, string password, bool lockoutOnFailure);
        Task<IList<Claim>> GetAllClaimsAsync(AppUser user);
        Task<IdentityResult> AddClaimAsync(AppUser user, Claim claim);
        Task<IList<string>> GetUserRolesAsync(AppUser user);
        Task<IdentityResult> SetUserRoleAsync(AppUser user, string role);
    }
}
