using Microsoft.EntityFrameworkCore;
using Users.DAL.Models;

namespace Users.DAL.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> opts) : base(opts) { }

        public DbSet<BaseUser> BaseUsers => Set<BaseUser>();
        public DbSet<EntryRequest> EntryRequests => Set<EntryRequest>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<University> Universities => Set<University>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BaseUser>()
                .HasOne(e => e.Teacher)
                .WithOne(e => e.BaseUser)
                .HasForeignKey<Teacher>(e => e.BaseUserId);

            modelBuilder.Entity<BaseUser>()
                .HasOne(e => e.Student)
                .WithOne(e => e.BaseUser)
                .HasForeignKey<Student>(e => e.BaseUserId);

            modelBuilder.Entity<BaseUser>()
                .HasMany(e => e.EntryRequests)
                .WithOne(e => e.BaseUser)
                .HasForeignKey(e => e.BaseUserId);

            modelBuilder.Entity<University>()
                .OwnsOne(e => e.Address);

            modelBuilder.Entity<University>()
                .HasOne(e => e.Director)
                .WithOne(e => e.UniversityDirector)
                .HasForeignKey<University>(e => e.DirectorId);

            modelBuilder.Entity<University>()
                .HasMany(e => e.Members)
                .WithOne(e => e.UniversityMember)
                .HasForeignKey(e => e.UniversityId);

            modelBuilder.Entity<University>()
                .HasMany(e => e.EntryRequests)
                .WithOne(e => e.University)
                .HasForeignKey(e => e.UniversityId);
        }
    }
}
