using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class SubmitQuizDto
    {
        [Required]
        public IEnumerable<SubmitQuizQuestionAnswerDto> Answers { get; set; } = new List<SubmitQuizQuestionAnswerDto>();
    }
}
