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
        private readonly IBaseRepository<LessonResource> _lessonResourceRepository;
        private readonly IBaseRepository<Course> _courseRepository;
        private readonly IBaseRepository<StudentLessonCompletion> _completionRepository;
        private readonly IUserInfoClient _grpcClient;
        private readonly IMarkdownService _markdownService;

        public LessonService(
            IBaseRepository<Lesson> lessonRepository,
            IBaseRepository<LessonResource> lessonResourceRepository,
            IBaseRepository<Course> courseRepository,
            IBaseRepository<StudentLessonCompletion> completionRepository,
            IUserInfoClient grpcClient,
            IMarkdownService markdownService)
        {
            _lessonRepository = lessonRepository;
            _lessonResourceRepository = lessonResourceRepository;
            _courseRepository = courseRepository;
            _completionRepository = completionRepository;
            _grpcClient = grpcClient;
            _markdownService = markdownService;
        }

        private static string NormalizeResourceType(string? type)
        {
            return string.IsNullOrWhiteSpace(type) ? "other" : type.Trim().ToLowerInvariant();
        }

        private static List<LessonResourceDto> NormalizeResourceDtos(IEnumerable<LessonResourceDto>? resources)
        {
            if (resources == null)
                return new List<LessonResourceDto>();

            return resources
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Url))
                .Select(r => new LessonResourceDto
                {
                    Title = string.IsNullOrWhiteSpace(r.Title) ? r.Url.Trim() : r.Title.Trim(),
                    Url = r.Url.Trim(),
                    Type = NormalizeResourceType(r.Type)
                })
                .GroupBy(r => r.Url, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        private static List<LessonResourceDto> MergeLegacyLinksWithResources(string? videoLink, string? materialLink, IEnumerable<LessonResourceDto>? resources)
        {
            var normalized = NormalizeResourceDtos(resources);

            if (!string.IsNullOrWhiteSpace(videoLink))
            {
                normalized.Add(new LessonResourceDto
                {
                    Title = "Video material",
                    Url = videoLink.Trim(),
                    Type = "video"
                });
            }

            if (!string.IsNullOrWhiteSpace(materialLink))
            {
                normalized.Add(new LessonResourceDto
                {
                    Title = "Additional material",
                    Url = materialLink.Trim(),
                    Type = "material"
                });
            }

            return normalized
                .GroupBy(r => r.Url, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        private static void ApplyLegacyLinksFromResources(Lesson lesson)
        {
            var firstVideo = lesson.Resources.FirstOrDefault(r => r.Type == "video");
            var firstNonVideo = lesson.Resources.FirstOrDefault(r => r.Type != "video");

            lesson.VideoLink = firstVideo?.Url;
            lesson.MaterialLink = firstNonVideo?.Url;
        }

        private static List<LessonResourceDto> MapResourcesForRead(Lesson lesson)
        {
            var resources = lesson.Resources
                .Select(resource => new LessonResourceDto
                {
                    Title = resource.Title,
                    Url = resource.Url,
                    Type = resource.Type
                })
                .ToList();

            return MergeLegacyLinksWithResources(lesson.VideoLink, lesson.MaterialLink, resources);
        }

        private async Task ReplaceLessonResourcesAsync(Lesson lesson, IEnumerable<LessonResourceDto> resources)
        {
            if (lesson.Resources.Any())
            {
                await _lessonResourceRepository.DeleteRangeAsync(lesson.Resources.ToList());
                lesson.Resources.Clear();
            }

            var normalizedResources = NormalizeResourceDtos(resources);

            foreach (var resourceDto in normalizedResources)
            {
                lesson.Resources.Add(new LessonResource
                {
                    LessonId = lesson.Id,
                    Title = resourceDto.Title ?? resourceDto.Url,
                    Url = resourceDto.Url,
                    Type = NormalizeResourceType(resourceDto.Type),
                    CreatedAt = DateTime.UtcNow,
                });
            }

            ApplyLegacyLinksFromResources(lesson);
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
                .Include(obj => obj.Resources)
                .OrderBy(obj => obj.OrderNumber)
                .ToListAsync();

            var lessonIds = lessons.Select(obj => obj.Id).ToList();
            var completions = await _completionRepository.Where(obj =>
                    obj.StudentId == reply.UserId && lessonIds.Contains(obj.LessonId))
                .ToListAsync();
            var completionMap = completions.ToDictionary(obj => obj.LessonId, obj => obj.CompletedAt);

            var lessonDtos = lessons.Select(lesson => new ReadLessonDto
            {
                Id = lesson.Id,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                Content = lesson.Content,
                IsMarkdown = lesson.IsMarkdown,
                VideoLink = lesson.VideoLink,
                MaterialLink = lesson.MaterialLink,
                Resources = MapResourcesForRead(lesson),
                OrderNumber = lesson.OrderNumber,
                CreatedAt = lesson.CreatedAt,
                UpdatedAt = lesson.UpdatedAt,
                IsCompletedByCurrentUser = completionMap.ContainsKey(lesson.Id),
                CompletedAt = completionMap.TryGetValue(lesson.Id, out var completedAt) ? completedAt : null
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
                .Include(obj => obj.Resources)
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

            var completion = await _completionRepository.SingleOrDefaultAsync(obj =>
                obj.StudentId == reply.UserId && obj.LessonId == lessonId);

            response.Success = true;
            response.Message = "Lesson retrieved successfully";
            response.Value = new ReadLessonDto
            {
                Id = lesson.Id,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                Content = lesson.Content,
                IsMarkdown = lesson.IsMarkdown,
                RenderedContent = lesson.IsMarkdown ? _markdownService.ConvertToHtml(lesson.Content) : null,
                VideoLink = lesson.VideoLink,
                MaterialLink = lesson.MaterialLink,
                Resources = MapResourcesForRead(lesson),
                OrderNumber = lesson.OrderNumber,
                CreatedAt = lesson.CreatedAt,
                UpdatedAt = lesson.UpdatedAt,
                IsCompletedByCurrentUser = completion != null,
                CompletedAt = completion?.CompletedAt
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
            if (reply == null || !reply.IsAllowed || (!reply.IsTeacher && !reply.IsDirector))
            {
                response.Message = "Only university teachers or directors can create lessons";
                return response;
            }

            Lesson lesson = new Lesson
            {
                Title = lessonDto.Title,
                Content = lessonDto.Content,
                IsMarkdown = lessonDto.IsMarkdown,
                OrderNumber = lessonDto.OrderNumber,
                CourseId = lessonDto.CourseId,
                CreatedAt = DateTime.UtcNow
            };

            await _lessonRepository.AddAsync(lesson);

            var mergedResources = MergeLegacyLinksWithResources(lessonDto.VideoLink, lessonDto.MaterialLink, lessonDto.Resources);
            await ReplaceLessonResourcesAsync(lesson, mergedResources);
            await _lessonRepository.UpdateAsync(lesson);

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
                .Include(obj => obj.Resources)
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
            if (reply == null || !reply.IsAllowed || (!reply.IsTeacher && !reply.IsDirector))
            {
                response.Message = "Only university teachers or directors can update lessons";
                return response;
            }

            if (!string.IsNullOrWhiteSpace(lessonDto.Title))
                lesson.Title = lessonDto.Title;

            if (!string.IsNullOrWhiteSpace(lessonDto.Content))
                lesson.Content = lessonDto.Content;

            if (lessonDto.OrderNumber.HasValue)
                lesson.OrderNumber = lessonDto.OrderNumber.Value;

            if (lessonDto.Resources != null || lessonDto.VideoLink != null || lessonDto.MaterialLink != null)
            {
                var mergedResources = MergeLegacyLinksWithResources(lessonDto.VideoLink, lessonDto.MaterialLink, lessonDto.Resources);
                await ReplaceLessonResourcesAsync(lesson, mergedResources);
            }

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
            if (reply == null || !reply.IsAllowed || (!reply.IsTeacher && !reply.IsDirector))
            {
                response.Message = "Only university teachers or directors can delete lessons";
                return response;
            }

            await _lessonRepository.DeleteAsync(lesson);
            response.Success = true;
            response.Message = "Lesson deleted successfully";

            return response;
        }

        public async Task<ResponseMessage> ReorderLessonsAsync(ReorderLessonsDto reorderDto, string teacherEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            if (reorderDto.Items == null || reorderDto.Items.Count == 0)
            {
                response.Message = "No items provided";
                return response;
            }

            var course = await _courseRepository.SingleOrDefaultAsync(obj => obj.Id == reorderDto.CourseId);
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
            if (reply == null || !reply.IsAllowed || (!reply.IsTeacher && !reply.IsDirector))
            {
                response.Message = "Only university teachers or directors can reorder lessons";
                return response;
            }

            var lessonIds = reorderDto.Items.Select(i => i.LessonId).ToList();
            var lessons = await _lessonRepository.Where(obj => obj.CourseId == reorderDto.CourseId && lessonIds.Contains(obj.Id))
                .ToListAsync();

            if (lessons.Count != reorderDto.Items.Count)
            {
                response.Message = "One or more lessons do not belong to this course";
                return response;
            }

            var orderMap = reorderDto.Items.ToDictionary(i => i.LessonId, i => i.OrderNumber);
            foreach (var lesson in lessons)
            {
                lesson.OrderNumber = orderMap[lesson.Id];
                lesson.UpdatedAt = DateTime.UtcNow;
                await _lessonRepository.UpdateAsync(lesson);
            }

            response.Success = true;
            response.Message = "Lessons reordered successfully";

            return response;
        }
    }
}
