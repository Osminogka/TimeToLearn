using Courses.DAL.Dtos;
using Courses.DAL.SideModels;

namespace Courses.DL.Services
{
    public interface IQuizService
    {
        Task<ResponseArray<QuizQuestionDto>> GetLessonQuizQuestionsAsync(long lessonId, string userEmail);
        Task<ResponseArray<QuizQuestionDto>> GetCourseQuizQuestionsAsync(long courseId, string userEmail);
        Task<ResponseMessage> UpsertLessonQuizAsync(long lessonId, UpsertQuizDto dto, string teacherEmail);
        Task<ResponseMessage> UpsertCourseQuizAsync(long courseId, UpsertQuizDto dto, string teacherEmail);
        Task<ResponseMessage> CreateLessonQuizQuestionAsync(long lessonId, CreateQuizQuestionDto dto, string teacherEmail);
        Task<ResponseMessage> UpdateLessonQuizQuestionAsync(long lessonId, long questionId, CreateQuizQuestionDto dto, string teacherEmail);
        Task<ResponseMessage> CreateCourseQuizQuestionAsync(long courseId, CreateQuizQuestionDto dto, string teacherEmail);
        Task<ResponseMessage> UpdateCourseQuizQuestionAsync(long courseId, long questionId, CreateQuizQuestionDto dto, string teacherEmail);
        Task<ResponseMessage> SubmitLessonQuizAsync(long lessonId, SubmitQuizDto dto, string userEmail);
        Task<ResponseMessage> SubmitCourseQuizAsync(long courseId, SubmitQuizDto dto, string userEmail);
        Task<ResponseArray<StudentQuizAnswerDto>> GetMyLessonQuizAnswersAsync(long lessonId, string userEmail);
        Task<ResponseArray<StudentQuizAnswerDto>> GetMyCourseQuizAnswersAsync(long courseId, string userEmail);
        Task<ResponseArray<QuizAnswerReviewDto>> GetLessonQuizAnswersForTeacherAsync(long lessonId, string teacherEmail);
        Task<ResponseArray<QuizAnswerReviewDto>> GetCourseQuizAnswersForTeacherAsync(long courseId, string teacherEmail);
    }
}
