using Microsoft.EntityFrameworkCore;
using Core.DAL.Models;

namespace Core.DAL.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> opts) : base(opts) { }

        public DbSet<BaseUser> BaseUsers => Set<BaseUser>();
        public DbSet<EntryRequest> EntryRequests => Set<EntryRequest>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<University> Universities => Set<University>();
        public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
        public DbSet<TeacherEnrollment> TeacherEnrollments => Set<TeacherEnrollment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BaseUser>()
                .HasOne(e => e.Teacher)
                .WithOne(e => e.BaseUser)
                .HasForeignKey<Teacher>(e => e.BaseUserId);

            modelBuilder.Entity<BaseUser>()
                .HasMany(e => e.EntryRequests)
                .WithOne(e => e.BaseUser)
                .HasForeignKey(e => e.BaseUserId);

            modelBuilder.Entity<BaseUser>()
                .OwnsOne(e => e.Address, p =>
                {
                    p.Property(p => p.City).IsRequired(false);
                    p.Property(p => p.Country).IsRequired(false);
                    p.Property(p => p.Street).IsRequired(false);
                });

            modelBuilder.Entity<University>()
                .OwnsOne(e => e.Address, p =>
                {
                    p.Property(p => p.City).IsRequired(false);
                    p.Property(p => p.Country).IsRequired(false);
                    p.Property(p => p.Street).IsRequired(false);
                });

            modelBuilder.Entity<University>()
                .HasOne(e => e.Director)
                .WithMany(e => e.DirectingUniversities)
                .HasForeignKey(e => e.DirectorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<University>()
                .HasMany(e => e.EntryRequests)
                .WithOne(e => e.University)
                .HasForeignKey(e => e.UniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentEnrollment>()
                .HasOne(e => e.BaseUser)
                .WithMany(e => e.StudentEnrollments)
                .HasForeignKey(e => e.BaseUserId);

            modelBuilder.Entity<StudentEnrollment>()
                .HasOne(e => e.University)
                .WithMany(e => e.StudentEnrollments)
                .HasForeignKey(e => e.UniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentEnrollment>()
                .HasIndex(e => new { e.BaseUserId, e.UniversityId })
                .IsUnique();

            modelBuilder.Entity<TeacherEnrollment>()
                .HasOne(e => e.BaseUser)
                .WithMany(e => e.TeacherEnrollments)
                .HasForeignKey(e => e.BaseUserId);

            modelBuilder.Entity<TeacherEnrollment>()
                .HasOne(e => e.University)
                .WithMany(e => e.TeacherEnrollments)
                .HasForeignKey(e => e.UniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeacherEnrollment>()
                .HasIndex(e => new { e.BaseUserId, e.UniversityId })
                .IsUnique();
        }
    }
}
