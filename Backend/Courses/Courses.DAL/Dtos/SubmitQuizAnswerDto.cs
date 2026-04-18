using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class SubmitQuizAnswerDto
    {
        [Required]
        public long SelectedOptionId { get; set; }
    }
}
