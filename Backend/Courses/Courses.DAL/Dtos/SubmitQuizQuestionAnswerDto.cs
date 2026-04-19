using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class SubmitQuizQuestionAnswerDto
    {
        [Required]
        public long QuestionId { get; set; }

        [Required]
        public long SelectedOptionId { get; set; }
    }
}
