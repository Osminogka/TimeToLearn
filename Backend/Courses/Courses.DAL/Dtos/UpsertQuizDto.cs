using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class UpsertQuizDto
    {
        [Required]
        public IEnumerable<CreateQuizQuestionDto> Questions { get; set; } = new List<CreateQuizQuestionDto>();
    }
}
