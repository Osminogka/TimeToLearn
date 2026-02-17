namespace Courses.DAL.Dtos
{
    public class UpdateLessonDto
    {
        public long LessonId { get; set; }

        public string? Title { get; set; }

        public string? Content { get; set; }

        public bool? IsMarkdown { get; set; }

        public string? VideoLink { get; set; }

        public string? MaterialLink { get; set; }

        public int? OrderNumber { get; set; }
    }
}

