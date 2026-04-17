namespace Courses.DAL.Dtos
{
    public class CourseProgressSummaryDto
    {
        public long CourseId { get; set; }

        public long StudentId { get; set; }

        public int TotalLessons { get; set; }

        public int CompletedLessons { get; set; }

        public int PercentComplete { get; set; }

        public bool IsCompleted { get; set; }

        public int? Mark { get; set; }

        public DateTime? MarkGivenAt { get; set; }

        public IEnumerable<long> CompletedLessonIds { get; set; } = new List<long>();
    }
}
