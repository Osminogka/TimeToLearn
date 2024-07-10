﻿using System.Security.Claims;
using Authentication.DAL.Models;
using Microsoft.AspNetCore.Identity;

namespace Authentication.DL.Repositories
{
    public interface IUsersRepository
    {
        IQueryable<AppUser> Get();
        AppUser GetByEmail(string email);
        Task<IdentityResult> Create(AppUser user, string password);
        Task<IdentityResult> Delete(AppUser user);
        Task<IdentityResult> Update(AppUser user);
        UserManager<AppUser> GetUserManager();
        Task<IdentityResult> AddClaimAsync(AppUser user, Claim claim);
        Task<IList<string>> GetUserRolesAsync(AppUser user);
        Task<IdentityResult> SetUserRoleAsync(AppUser user, string role);
    }
}
