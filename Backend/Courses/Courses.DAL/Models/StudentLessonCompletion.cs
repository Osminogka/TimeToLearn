using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Models
{
    public class StudentLessonCompletion : BaseEntity
    {
        [Required]
        public long StudentId { get; set; }

        [Required]
        public long LessonId { get; set; }

        public Lesson Lesson { get; set; }

        public DateTime CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
