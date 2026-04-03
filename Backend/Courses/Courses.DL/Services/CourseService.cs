using Courses.DAL.Dtos;
using Courses.DAL.Models;
using Courses.DAL.SideModels;
using Courses.DL.Grpc;
using Courses.DL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Courses.DL.Services
{
    public class CourseService : ICourseService
    {
        private readonly IBaseRepository<Course> _courseRepository;
        private readonly IUserInfoClient _grpcClient;

        public CourseService(IBaseRepository<Course> courseRepository, IUserInfoClient grpcClient)
        {
            _courseRepository = courseRepository;
            _grpcClient = grpcClient;
        }

        public async Task<ResponseArray<ReadCourseDto>> GetUniversityCoursesAsync(string universityName, string userEmail, int page)
        {
            ResponseArray<ReadCourseDto> response = new ResponseArray<ReadCourseDto>();
            response.Message = "You don't have such rights";
            const int courseNumbers = 10;

            if (page < 0)
            {
                response.Message = "Invalid page number";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForCourse(universityName, userEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            var courses = await _courseRepository.Where(obj => obj.UniversityId == reply.UniversityId)
                .Include(obj => obj.Lessons)
                .Skip(page * courseNumbers)
                .Take(courseNumbers)
                .ToListAsync();

            var teacherIds = courses.Select(c => c.TeacherId).Distinct().ToList();
            var nameEntries = await Task.WhenAll(
                teacherIds.Select(async id => (id, name: await _grpcClient.GetUserName(id))));
            var teacherNames = nameEntries.ToDictionary(x => x.id, x => x.name ?? "Unknown");

            var courseDtos = courses.Select(course => new ReadCourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                TeacherId = course.TeacherId,
                TeacherName = teacherNames[course.TeacherId],
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                LessonsCount = course.Lessons?.Count ?? 0
            }).ToList();

            response.Success = true;
            response.Message = "You got some courses";
            response.Values = courseDtos;

            return response;
        }

        public async Task<ResponseWithValue<ReadCourseDto>> GetCourseAsync(long courseId, string userEmail)
        {
            ResponseWithValue<ReadCourseDto> response = new ResponseWithValue<ReadCourseDto>();
            response.Message = "You don't have such rights";

            var course = await _courseRepository.Where(obj => obj.Id == courseId)
                .Include(obj => obj.Lessons)
                .FirstOrDefaultAsync();

            if (course == null)
            {
                response.Message = "Such course doesn't exist";
                return response;
            }

            var universityName = await _grpcClient.GetUniversityName(course.UniversityId);
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForCourse(universityName, userEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            var teacherName = await _grpcClient.GetUserName(course.TeacherId);

            response.Success = true;
            response.Message = "Course retrieved successfully";
            response.Value = new ReadCourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                TeacherId = course.TeacherId,
                TeacherName = teacherName ?? "Unknown",
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                LessonsCount = course.Lessons?.Count ?? 0
            };

            return response;
        }

        public async Task<ResponseMessage> CreateCourseAsync(CreateCourseDto courseDto, string teacherEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            if (string.IsNullOrWhiteSpace(courseDto.Title))
            {
                response.Message = "Course title cannot be empty";
                return response;
            }

            if (string.IsNullOrWhiteSpace(courseDto.Description))
            {
                response.Message = "Course description cannot be empty";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForCourse(courseDto.UniversityName, teacherEmail);
            if (reply == null || !reply.IsAllowed || !reply.IsTeacher)
            {
                response.Message = "Only teachers can create courses";
                return response;
            }

            Course course = new Course
            {
                Title = courseDto.Title,
                Description = courseDto.Description,
                TeacherId = reply.UserId,
                UniversityId = reply.UniversityId,
                CreatedAt = DateTime.UtcNow
            };

            await _courseRepository.AddAsync(course);
            response.Success = true;
            response.Message = "Course created successfully";

            return response;
        }

        public async Task<ResponseMessage> UpdateCourseAsync(UpdateCourseDto courseDto, string teacherEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var course = await _courseRepository.SingleOrDefaultAsync(obj => obj.Id == courseDto.CourseId);
            if (course == null)
            {
                response.Message = "Such course doesn't exist";
                return response;
            }

            var universityName = await _grpcClient.GetUniversityName(course.UniversityId);
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForCourse(universityName, teacherEmail);
            if (reply == null || !reply.IsAllowed || !reply.IsTeacher)
            {
                response.Message = "Only teachers can update courses";
                return response;
            }

            if (course.TeacherId != reply.UserId)
            {
                response.Message = "You can only update your own courses";
                return response;
            }

            if (!string.IsNullOrWhiteSpace(courseDto.Title))
                course.Title = courseDto.Title;

            if (!string.IsNullOrWhiteSpace(courseDto.Description))
                course.Description = courseDto.Description;

            course.UpdatedAt = DateTime.UtcNow;

            await _courseRepository.UpdateAsync(course);
            response.Success = true;
            response.Message = "Course updated successfully";

            return response;
        }

        public async Task<ResponseMessage> DeleteCourseAsync(long courseId, string teacherEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var course = await _courseRepository.SingleOrDefaultAsync(obj => obj.Id == courseId);
            if (course == null)
            {
                response.Message = "Such course doesn't exist";
                return response;
            }

            var universityName = await _grpcClient.GetUniversityName(course.UniversityId);
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForCourse(universityName, teacherEmail);
            if (reply == null || !reply.IsAllowed || !reply.IsTeacher)
            {
                response.Message = "Only teachers can delete courses";
                return response;
            }

            if (course.TeacherId != reply.UserId)
            {
                response.Message = "You can only delete your own courses";
                return response;
            }

            await _courseRepository.DeleteAsync(course);
            response.Success = true;
            response.Message = "Course deleted successfully";

            return response;
        }
    }
}
