using Courses.DAL.Dtos;
using Courses.DAL.SideModels;

namespace Courses.DL.Services
{
    public interface IQuizService
    {
        Task<ResponseArray<QuizQuestionDto>> GetLessonQuizQuestionsAsync(long lessonId, string userEmail);
        Task<ResponseArray<QuizQuestionDto>> GetCourseQuizQuestionsAsync(long courseId, string userEmail);
        Task<ResponseMessage> CreateLessonQuizQuestionAsync(long lessonId, CreateQuizQuestionDto dto, string teacherEmail);
        Task<ResponseMessage> CreateCourseQuizQuestionAsync(long courseId, CreateQuizQuestionDto dto, string teacherEmail);
        Task<ResponseMessage> SubmitLessonQuizAnswerAsync(long lessonId, long questionId, SubmitQuizAnswerDto dto, string userEmail);
        Task<ResponseMessage> SubmitCourseQuizAnswerAsync(long courseId, long questionId, SubmitQuizAnswerDto dto, string userEmail);
        Task<ResponseArray<StudentQuizAnswerDto>> GetMyLessonQuizAnswersAsync(long lessonId, string userEmail);
        Task<ResponseArray<StudentQuizAnswerDto>> GetMyCourseQuizAnswersAsync(long courseId, string userEmail);
        Task<ResponseArray<QuizAnswerReviewDto>> GetLessonQuizAnswersForTeacherAsync(long lessonId, string teacherEmail);
        Task<ResponseArray<QuizAnswerReviewDto>> GetCourseQuizAnswersForTeacherAsync(long courseId, string teacherEmail);
    }
}
