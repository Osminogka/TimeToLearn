using Authentication.DAL.Contexts;
using Authentication.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.API.Infrastructure
{
    public static class PrepDb
    {
        public async static Task PrepMemberRoles(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                await SeedData(serviceScope);
            }
        }

        private async static Task SeedData(IServiceScope serviceScope)
        {
            Console.WriteLine("--> Preparing database...");

            serviceScope.ServiceProvider.GetRequiredService<IdentityContext>().Database.Migrate();

            var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync(Roles.Student))
                await roleManager.CreateAsync(new IdentityRole(Roles.Student));
            if (!await roleManager.RoleExistsAsync(Roles.Teacher))
                await roleManager.CreateAsync(new IdentityRole(Roles.Teacher));
        }
    }
}
