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

            modelBuilder.Entity<Like>()
                .HasOne(e => e.Topic)
                .WithMany(e => e.Likes)
                .HasForeignKey(e => e.PostId);

            modelBuilder.Entity<Like>()
                .HasOne(e => e.Comment)
                .WithMany(e => e.Likes)
                .HasForeignKey(e => e.PostId);

            modelBuilder.Entity<Dislike>()
                .HasOne(e => e.Topic)
                .WithMany(e => e.Dislikes)
                .HasForeignKey(e => e.PostId);

            modelBuilder.Entity<Dislike>()
                .HasOne(e => e.Comment)
                .WithMany(e => e.Dislikes)
                .HasForeignKey(e => e.PostId);
        }
    }
}
