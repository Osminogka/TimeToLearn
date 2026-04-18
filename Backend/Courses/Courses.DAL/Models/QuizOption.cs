using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Models
{
    public class QuizOption : BaseEntity
    {
        [Required]
        public long QuizQuestionId { get; set; }

        public QuizQuestion QuizQuestion { get; set; }

        [Required]
        [MaxLength(300)]
        public string OptionText { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
