namespace Courses.DAL.Dtos
{
    public class LessonProgressDto
    {
        public long LessonId { get; set; }

        public long StudentId { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
