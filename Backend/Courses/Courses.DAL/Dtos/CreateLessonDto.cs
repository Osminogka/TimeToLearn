using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class CreateLessonDto
    {
        [Required]
        public long CourseId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsMarkdown { get; set; } = true;

        [Url]
        [MaxLength(500)]
        public string? VideoLink { get; set; }

        [Url]
        [MaxLength(500)]
        public string? MaterialLink { get; set; }

        [Required]
        public int OrderNumber { get; set; }
    }
}

