using Courses.DAL.Dtos;
using Courses.DAL.Models;
using Courses.DAL.SideModels;
using Courses.DL.Grpc;
using Courses.DL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Courses.DL.Services
{
    public class LessonService : ILessonService
    {
        private readonly IBaseRepository<Lesson> _lessonRepository;
        private readonly IBaseRepository<Course> _courseRepository;
        private readonly IUserInfoClient _grpcClient;
        private readonly IMarkdownService _markdownService;

        public LessonService(IBaseRepository<Lesson> lessonRepository, IBaseRepository<Course> courseRepository, IUserInfoClient grpcClient, IMarkdownService markdownService)
        {
            _lessonRepository = lessonRepository;
            _courseRepository = courseRepository;
            _grpcClient = grpcClient;
            _markdownService = markdownService;
        }


        public async Task<ResponseArray<ReadLessonDto>> GetCourseLessonsAsync(long courseId, string userEmail)
        {
            ResponseArray<ReadLessonDto> response = new ResponseArray<ReadLessonDto>();
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

            var reply = await _grpcClient.GetUserInfoForCourse(universityName, userEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            var lessons = await _lessonRepository.Where(obj => obj.CourseId == courseId)
                .OrderBy(obj => obj.OrderNumber)
                .ToListAsync();

            var lessonDtos = lessons.Select(lesson => new ReadLessonDto
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Content = lesson.Content,
                IsMarkdown = lesson.IsMarkdown,
                RenderedContent = lesson.IsMarkdown ? _markdownService.ConvertToHtml(lesson.Content) : null,
                VideoLink = lesson.VideoLink,
                MaterialLink = lesson.MaterialLink,
                OrderNumber = lesson.OrderNumber,
                CreatedAt = lesson.CreatedAt,
                UpdatedAt = lesson.UpdatedAt
            }).ToList();


            response.Success = true;
            response.Message = "Lessons retrieved successfully";
            response.Values = lessonDtos;

            return response;
        }

        public async Task<ResponseWithValue<ReadLessonDto>> GetLessonAsync(long lessonId, string userEmail)
        {
            ResponseWithValue<ReadLessonDto> response = new ResponseWithValue<ReadLessonDto>();
            response.Message = "You don't have such rights";

            var lesson = await _lessonRepository.Where(obj => obj.Id == lessonId)
                .Include(obj => obj.Course)
                .FirstOrDefaultAsync();

            if (lesson == null)
            {
                response.Message = "Such lesson doesn't exist";
                return response;
            }

            var universityName = await _grpcClient.GetUniversityName(lesson.Course.UniversityId);
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForCourse(universityName, userEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            response.Success = true;
            response.Message = "Lesson retrieved successfully";
            response.Value = new ReadLessonDto
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Content = lesson.Content,
                IsMarkdown = lesson.IsMarkdown,
                RenderedContent = lesson.IsMarkdown ? _markdownService.ConvertToHtml(lesson.Content) : null,
                VideoLink = lesson.VideoLink,
                MaterialLink = lesson.MaterialLink,
                OrderNumber = lesson.OrderNumber,
                CreatedAt = lesson.CreatedAt,
                UpdatedAt = lesson.UpdatedAt
            };

            return response;

        }

        public async Task<ResponseMessage> CreateLessonAsync(CreateLessonDto lessonDto, string teacherEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            if (string.IsNullOrWhiteSpace(lessonDto.Title))
            {
                response.Message = "Lesson title cannot be empty";
                return response;
            }

            if (string.IsNullOrWhiteSpace(lessonDto.Content))
            {
                response.Message = "Lesson content cannot be empty";
                return response;
            }

            var course = await _courseRepository.SingleOrDefaultAsync(obj => obj.Id == lessonDto.CourseId);
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
                response.Message = "Only teachers can create lessons";
                return response;
            }

            if (course.TeacherId != reply.UserId)
            {
                response.Message = "You can only add lessons to your own courses";
                return response;
            }

            Lesson lesson = new Lesson
            {
                Title = lessonDto.Title,
                Content = lessonDto.Content,
                VideoLink = lessonDto.VideoLink,
                MaterialLink = lessonDto.MaterialLink,
                OrderNumber = lessonDto.OrderNumber,
                CourseId = lessonDto.CourseId,
                CreatedAt = DateTime.UtcNow
            };

            await _lessonRepository.AddAsync(lesson);
            response.Success = true;
            response.Message = "Lesson created successfully";

            return response;
        }

        public async Task<ResponseMessage> UpdateLessonAsync(UpdateLessonDto lessonDto, string teacherEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var lesson = await _lessonRepository.Where(obj => obj.Id == lessonDto.LessonId)
                .Include(obj => obj.Course)
                .FirstOrDefaultAsync();

            if (lesson == null)
            {
                response.Message = "Such lesson doesn't exist";
                return response;
            }

            var universityName = await _grpcClient.GetUniversityName(lesson.Course.UniversityId);
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForCourse(universityName, teacherEmail);
            if (reply == null || !reply.IsAllowed || !reply.IsTeacher)
            {
                response.Message = "Only teachers can update lessons";
                return response;
            }

            if (lesson.Course.TeacherId != reply.UserId)
            {
                response.Message = "You can only update lessons in your own courses";
                return response;
            }

            if (!string.IsNullOrWhiteSpace(lessonDto.Title))
                lesson.Title = lessonDto.Title;

            if (!string.IsNullOrWhiteSpace(lessonDto.Content))
                lesson.Content = lessonDto.Content;

            if (lessonDto.VideoLink != null)
                lesson.VideoLink = lessonDto.VideoLink;

            if (lessonDto.MaterialLink != null)
                lesson.MaterialLink = lessonDto.MaterialLink;

            if (lessonDto.OrderNumber.HasValue)
                lesson.OrderNumber = lessonDto.OrderNumber.Value;

            lesson.UpdatedAt = DateTime.UtcNow;

            await _lessonRepository.UpdateAsync(lesson);
            response.Success = true;
            response.Message = "Lesson updated successfully";

            return response;
        }

        public async Task<ResponseMessage> DeleteLessonAsync(long lessonId, string teacherEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var lesson = await _lessonRepository.Where(obj => obj.Id == lessonId)
                .Include(obj => obj.Course)
                .FirstOrDefaultAsync();

            if (lesson == null)
            {
                response.Message = "Such lesson doesn't exist";
                return response;
            }

            var universityName = await _grpcClient.GetUniversityName(lesson.Course.UniversityId);
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForCourse(universityName, teacherEmail);
            if (reply == null || !reply.IsAllowed || !reply.IsTeacher)
            {
                response.Message = "Only teachers can delete lessons";
                return response;
            }

            if (lesson.Course.TeacherId != reply.UserId)
            {
                response.Message = "You can only delete lessons from your own courses";
                return response;
            }

            await _lessonRepository.DeleteAsync(lesson);
            response.Success = true;
            response.Message = "Lesson deleted successfully";

            return response;
        }
    }
}
