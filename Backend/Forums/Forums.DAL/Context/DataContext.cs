using Microsoft.EntityFrameworkCore;
using Forums.DAL.Models;

namespace Forums.DAL.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> opts) : base(opts) { }

        public DbSet<Topic> Topics => Set<Topic>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<Dislike> Dislikes => Set<Dislike>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Issue 7: Both Like→Topic and Like→Comment used PostId as FK — last config wins and silently
            // drops the first. Navigation properties are kept without explicit FK config; the IsTopic flag
            // enforces which PostId refers to at the service layer.

            modelBuilder.Entity<Like>()
                .HasIndex(e => new { e.UserId, e.PostId, e.IsTopic })
                .IsUnique();

            modelBuilder.Entity<Dislike>()
                .HasIndex(e => new { e.UserId, e.PostId, e.IsTopic })
                .IsUnique();
        }
    }
}
