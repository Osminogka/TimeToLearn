using Courses.DAL.Dtos;
using Courses.DAL.SideModels;

namespace Courses.DL.Services
{
    public interface ILessonService
    {
        Task<ResponseArray<ReadLessonDto>> GetCourseLessonsAsync(long courseId, string userEmail);
        Task<ResponseWithValue<ReadLessonDto>> GetLessonAsync(long lessonId, string userEmail);
        Task<ResponseMessage> CreateLessonAsync(CreateLessonDto lessonDto, string teacherEmail);
        Task<ResponseMessage> UpdateLessonAsync(UpdateLessonDto lessonDto, string teacherEmail);
        Task<ResponseMessage> DeleteLessonAsync(long lessonId, string teacherEmail);
    }
}
