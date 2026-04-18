namespace Courses.DAL.Dtos
{
    public class QuizQuestionDto
    {
        public long Id { get; set; }

        public long CourseId { get; set; }

        public long? LessonId { get; set; }

        public bool IsCourseLevel { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string AttemptPolicy { get; set; } = "reattempt";

        public IEnumerable<QuizOptionDto> Options { get; set; } = new List<QuizOptionDto>();
    }
}
