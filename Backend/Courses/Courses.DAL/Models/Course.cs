using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Models
{
    public class Course : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public long TeacherId { get; set; }

        [Required]
        public long UniversityId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Lesson> Lessons { get; set; }
    }
}
