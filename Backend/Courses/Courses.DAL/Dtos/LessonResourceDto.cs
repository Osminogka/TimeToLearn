using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class LessonResourceDto
    {
        [MaxLength(120)]
        public string? Title { get; set; }

        [Required]
        [Url]
        [MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? Type { get; set; }
    }
}
