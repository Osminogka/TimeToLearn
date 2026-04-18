using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Models
{
    public class QuizQuestion : BaseEntity
    {
        [Required]
        public long CourseId { get; set; }

        public Course Course { get; set; }

        public long? LessonId { get; set; }

        public Lesson? Lesson { get; set; }

        [Required]
        [MaxLength(400)]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string AttemptPolicy { get; set; } = "reattempt";

        public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();

        public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
