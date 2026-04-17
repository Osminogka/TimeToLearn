namespace Courses.DAL.Dtos
{
    public class ReadCourseDto
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public long TeacherId { get; set; }

        public string TeacherName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int LessonsCount { get; set; }

        public int CompletedLessonsCount { get; set; }

        public int CompletionPercent { get; set; }

        public bool IsCompletedByCurrentUser { get; set; }

        public int? CurrentUserMark { get; set; }

        public DateTime? CurrentUserMarkGivenAt { get; set; }
    }
}
