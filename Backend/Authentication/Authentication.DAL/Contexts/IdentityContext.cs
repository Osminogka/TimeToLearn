using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Authentication.DAL.Models;
using System.Security.Principal;

namespace Authentication.DAL.Contexts
{
    public class IdentityContext : IdentityDbContext
    {
        public IdentityContext(DbContextOptions<IdentityContext> options)
            : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Student>(entity => { entity.ToTable("Students"); });
            builder.Entity<Teacher>(entity => { entity.ToTable("Teachers"); });
            builder.Entity<AppUser>(entity => { entity.ToTable("AppUsers"); });
        }
    }
}
