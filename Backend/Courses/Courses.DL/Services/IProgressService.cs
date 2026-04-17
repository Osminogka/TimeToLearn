using Courses.DAL.Dtos;
using Courses.DAL.SideModels;

namespace Courses.DL.Services
{
    public interface IProgressService
    {
        Task<ResponseMessage> CompleteLessonAsync(long lessonId, string userEmail);
        Task<ResponseWithValue<LessonProgressDto>> GetLessonProgressAsync(long lessonId, string userEmail);
        Task<ResponseWithValue<CourseProgressSummaryDto>> GetCourseProgressAsync(long courseId, string userEmail);
        Task<ResponseArray<StudentCourseProgressDto>> GetCourseStudentsProgressAsync(long courseId, string teacherEmail);
        Task<ResponseMessage> AssignCourseGradeAsync(long courseId, AssignCourseGradeDto gradeDto, string teacherEmail);
    }
}
