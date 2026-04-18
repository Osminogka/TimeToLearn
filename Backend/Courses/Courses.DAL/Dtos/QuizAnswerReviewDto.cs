namespace Courses.DAL.Dtos
{
    public class QuizAnswerReviewDto
    {
        public long StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public long QuestionId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string SelectedOptionText { get; set; } = string.Empty;

        public string CorrectOptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public DateTime AnsweredAt { get; set; }
    }
}
