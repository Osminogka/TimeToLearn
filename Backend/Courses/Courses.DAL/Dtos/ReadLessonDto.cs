namespace Courses.DAL.Dtos
{
    public class ReadLessonDto
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public bool IsMarkdown { get; set; }

        public string? RenderedContent { get; set; }

        public string? VideoLink { get; set; }

        public string? MaterialLink { get; set; }

        public int OrderNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}

