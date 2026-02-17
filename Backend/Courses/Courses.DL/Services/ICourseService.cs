using Courses.DAL.Dtos;
using Courses.DAL.SideModels;

namespace Courses.DL.Services
{
    public interface ICourseService
    {
        Task<ResponseArray<ReadCourseDto>> GetUniversityCoursesAsync(string universityName, string userEmail, int page);
        Task<ResponseWithValue<ReadCourseDto>> GetCourseAsync(long courseId, string userEmail);
        Task<ResponseMessage> CreateCourseAsync(CreateCourseDto courseDto, string teacherEmail);
        Task<ResponseMessage> UpdateCourseAsync(UpdateCourseDto courseDto, string teacherEmail);
        Task<ResponseMessage> DeleteCourseAsync(long courseId, string teacherEmail);
    }
}
