using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Models
{
    public class Lesson : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public bool IsMarkdown { get; set; } = true;

        [MaxLength(500)]
        public string? VideoLink { get; set; }

        [MaxLength(500)]
        public string? MaterialLink { get; set; }

        [Required]
        public int OrderNumber { get; set; }

        [Required]
        public long CourseId { get; set; }

        public Course Course { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}

