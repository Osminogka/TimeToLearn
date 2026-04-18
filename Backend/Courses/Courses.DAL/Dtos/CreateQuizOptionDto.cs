using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class CreateQuizOptionDto
    {
        [Required]
        [MaxLength(300)]
        public string OptionText { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; }
    }
}
