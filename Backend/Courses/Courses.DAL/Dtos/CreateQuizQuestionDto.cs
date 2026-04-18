using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class CreateQuizQuestionDto
    {
        [Required]
        [MaxLength(400)]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        public IEnumerable<CreateQuizOptionDto> Options { get; set; } = new List<CreateQuizOptionDto>();

        [MaxLength(32)]
        public string AttemptPolicy { get; set; } = "reattempt";
    }
}
