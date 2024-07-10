using System.Security.Claims;
using Authentication.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace Authentication.DL.Repositories
{
    public class UsersRepository : IUsersRepository
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<AppUser> _signInManager;

        public UsersRepository(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        public IQueryable<AppUser> Get() => _userManager.Users;

        public async Task<AppUser?> GetByEmailAsync(string email) => await _userManager.FindByEmailAsync(email);

        public async Task<IdentityResult> CreateAsync(AppUser user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
                return await _userManager.AddToRoleAsync(user, Roles.Student);
            return IdentityResult.Failed();
        }

        public async Task<IdentityResult> DeleteAsync(AppUser user)
        {
            return await _userManager.DeleteAsync(user);
        }

        public async Task<IdentityResult> UpdateAsync(AppUser user)
        {
            return await _userManager.UpdateAsync(user);
        }

        public UserManager<AppUser> GetUserManager()
        {
            return _userManager;
        }

        public async Task<SignInResult> CheckPasswordSignInAsync(AppUser user, string password, bool lockoutOnFailure)
        {
            return await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        }

        public async Task<IList<Claim>> GetAllClaimsAsync(AppUser user) => await _userManager.GetClaimsAsync(user);

        public async Task<IdentityResult> AddClaimAsync(AppUser user, Claim claim) => await _userManager.AddClaimAsync(user, claim);

        public async Task<IList<string>> GetUserRolesAsync(AppUser user) => await _userManager.GetRolesAsync(user);

        public async Task<IdentityResult> SetUserRoleAsync(AppUser user, string role)
        {
            if (await _roleManager.RoleExistsAsync(role))
                return await _userManager.AddToRoleAsync(user, role);

            return IdentityResult.Failed();
        }
    }
}
