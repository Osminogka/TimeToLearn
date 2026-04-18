using Microsoft.EntityFrameworkCore;
using Courses.DAL.Models;

namespace Courses.DAL.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> opts) : base(opts) { }

        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<LessonResource> LessonResources => Set<LessonResource>();
        public DbSet<StudentLessonCompletion> StudentLessonCompletions => Set<StudentLessonCompletion>();
        public DbSet<StudentCourseGrade> StudentCourseGrades => Set<StudentCourseGrade>();
        public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
        public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
        public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>()
                .HasMany(e => e.Lessons)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Lesson>()
                .HasMany(e => e.Resources)
                .WithOne(e => e.Lesson)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentLessonCompletion>()
                .HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentLessonCompletion>()
                .HasIndex(e => new { e.StudentId, e.LessonId })
                .IsUnique();

            modelBuilder.Entity<StudentLessonCompletion>()
                .HasIndex(e => e.StudentId);

            modelBuilder.Entity<StudentLessonCompletion>()
                .HasIndex(e => e.LessonId);

            modelBuilder.Entity<StudentCourseGrade>()
                .HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentCourseGrade>()
                .HasIndex(e => new { e.StudentId, e.CourseId })
                .IsUnique();

            modelBuilder.Entity<StudentCourseGrade>()
                .HasIndex(e => e.StudentId);

            modelBuilder.Entity<StudentCourseGrade>()
                .HasIndex(e => e.CourseId);

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<QuizQuestion>()
                .HasMany(e => e.Options)
                .WithOne(e => e.QuizQuestion)
                .HasForeignKey(e => e.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizQuestion>()
                .HasMany(e => e.Answers)
                .WithOne(e => e.QuizQuestion)
                .HasForeignKey(e => e.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizQuestion>()
                .HasIndex(e => new { e.CourseId, e.LessonId });

            modelBuilder.Entity<QuizOption>()
                .HasIndex(e => e.QuizQuestionId);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(e => e.SelectedOption)
                .WithMany()
                .HasForeignKey(e => e.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizAnswer>()
                .HasIndex(e => new { e.StudentId, e.QuizQuestionId })
                .IsUnique();

            modelBuilder.Entity<QuizAnswer>()
                .HasIndex(e => e.StudentId);
        }
    }
}
