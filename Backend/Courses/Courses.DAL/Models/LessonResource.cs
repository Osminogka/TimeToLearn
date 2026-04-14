using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Models
{
    public class LessonResource : BaseEntity
    {
        [Required]
        public long LessonId { get; set; }

        public Lesson Lesson { get; set; }

        [Required]
        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string Type { get; set; } = "other";

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
