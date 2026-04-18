namespace Courses.DAL.Dtos
{
    public class QuizOptionDto
    {
        public long Id { get; set; }

        public string OptionText { get; set; } = string.Empty;

        public bool? IsCorrect { get; set; }
    }
}
