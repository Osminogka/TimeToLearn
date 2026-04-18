using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Models
{
    public class QuizAnswer : BaseEntity
    {
        [Required]
        public long QuizQuestionId { get; set; }

        public QuizQuestion QuizQuestion { get; set; }

        [Required]
        public long SelectedOptionId { get; set; }

        public QuizOption SelectedOption { get; set; }

        [Required]
        public long StudentId { get; set; }

        [Required]
        public bool IsCorrect { get; set; }

        public DateTime AnsweredAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
