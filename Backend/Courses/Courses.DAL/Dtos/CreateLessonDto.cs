namespace Courses.DAL.Dtos
{
    public class CreateLessonDto
    {
        public long CourseId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public bool IsMarkdown { get; set; } = true;

        public string? VideoLink { get; set; }

        public string? MaterialLink { get; set; }

        public int OrderNumber { get; set; }
    }
}

