namespace Courses.DAL.Dtos
{
    public class ReadLessonDto
    {
        public long Id { get; set; }

        public long CourseId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public bool IsMarkdown { get; set; }

        public string? RenderedContent { get; set; }

        public string? VideoLink { get; set; }

        public string? MaterialLink { get; set; }

        public IEnumerable<LessonResourceDto> Resources { get; set; } = new List<LessonResourceDto>();

        public int OrderNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}

