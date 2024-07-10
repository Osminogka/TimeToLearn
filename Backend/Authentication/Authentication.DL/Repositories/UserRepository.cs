using System.Security.Claims;
using Authentication.DAL.Models;
using Microsoft.AspNetCore.Identity;


namespace Authentication.DL.Repositories
{
    public class UsersRepository : IUsersRepository
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersRepository(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IQueryable<AppUser> Get() => _userManager.Users;

        public AppUser GetByEmail(string email) => _userManager.Users.First(u => u.Email == email);

        public Task<IdentityResult> Create(AppUser user, string password)
        {
            return _userManager.CreateAsync(user, password);
        }

        public async Task<IdentityResult> Delete(AppUser user)
        {
            return await _userManager.DeleteAsync(user);
        }

        public async Task<IdentityResult> Update(AppUser user)
        {
            return await _userManager.UpdateAsync(user);
        }

        public UserManager<AppUser> GetUserManager()
        {
            return _userManager;
        }

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
