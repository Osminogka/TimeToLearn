using Authentication.DAL.Contexts;
using Authentication.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.API.Infrastructure
{
    public static class PrepDb
    {
        public static void PrepMemberRoles(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                SeedData(serviceScope);
            }
        }

        private static async void SeedData(IServiceScope serviceScope)
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
