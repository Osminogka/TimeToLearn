namespace Courses.DAL.Dtos
{
    public class StudentQuizAnswerDto
    {
        public long QuestionId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public long SelectedOptionId { get; set; }

        public string SelectedOptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public DateTime AnsweredAt { get; set; }
    }
}
