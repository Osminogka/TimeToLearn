namespace Courses.DAL.Dtos
{
    public class StudentCourseProgressDto
    {
        public long StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public int TotalLessons { get; set; }

        public int CompletedLessons { get; set; }

        public int PercentComplete { get; set; }

        public bool IsCompleted { get; set; }

        public int? Mark { get; set; }

        public DateTime? MarkGivenAt { get; set; }
    }
}
