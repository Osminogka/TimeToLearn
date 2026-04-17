using Courses.DAL.Dtos;
using Courses.DAL.Models;
using Courses.DAL.SideModels;
using Courses.DL.Grpc;
using Courses.DL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Courses.DL.Services
{
    public class ProgressService : IProgressService
    {
        private readonly IBaseRepository<Course> _courseRepository;
        private readonly IBaseRepository<Lesson> _lessonRepository;
        private readonly IBaseRepository<StudentLessonCompletion> _completionRepository;
        private readonly IBaseRepository<StudentCourseGrade> _gradeRepository;
        private readonly IUserInfoClient _grpcClient;

        public ProgressService(
            IBaseRepository<Course> courseRepository,
            IBaseRepository<Lesson> lessonRepository,
            IBaseRepository<StudentLessonCompletion> completionRepository,
            IBaseRepository<StudentCourseGrade> gradeRepository,
            IUserInfoClient grpcClient)
        {
            _courseRepository = courseRepository;
            _lessonRepository = lessonRepository;
            _completionRepository = completionRepository;
            _gradeRepository = gradeRepository;
            _grpcClient = grpcClient;
        }

        public async Task<ResponseMessage> CompleteLessonAsync(long lessonId, string userEmail)
        {
            var response = new ResponseMessage
            {
                Message = "You don't have such rights"
            };

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

            if (reply.IsTeacher || reply.IsDirector)
            {
                response.Message = "Only students can complete lessons";
                return response;
            }

            var existingCompletion = await _completionRepository.SingleOrDefaultAsync(obj =>
                obj.StudentId == reply.UserId && obj.LessonId == lessonId);

            if (existingCompletion != null)
            {
                response.Success = true;
                response.Message = "Lesson already completed";
                return response;
            }

            await _completionRepository.AddAsync(new StudentLessonCompletion
            {
                StudentId = reply.UserId,
                LessonId = lessonId,
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

            response.Success = true;
            response.Message = "Lesson completed successfully";
            return response;
        }

        public async Task<ResponseWithValue<LessonProgressDto>> GetLessonProgressAsync(long lessonId, string userEmail)
        {
            var response = new ResponseWithValue<LessonProgressDto>
            {
                Message = "You don't have such rights"
            };

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

            var completion = await _completionRepository.SingleOrDefaultAsync(obj =>
                obj.StudentId == reply.UserId && obj.LessonId == lessonId);

            response.Success = true;
            response.Message = "Lesson progress retrieved successfully";
            response.Value = new LessonProgressDto
            {
                LessonId = lessonId,
                StudentId = reply.UserId,
                IsCompleted = completion != null,
                CompletedAt = completion?.CompletedAt
            };

            return response;
        }

        public async Task<ResponseWithValue<CourseProgressSummaryDto>> GetCourseProgressAsync(long courseId, string userEmail)
        {
            var response = new ResponseWithValue<CourseProgressSummaryDto>
            {
                Message = "You don't have such rights"
            };

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

            var lessonIds = await _lessonRepository.Where(obj => obj.CourseId == courseId)
                .Select(obj => obj.Id)
                .ToListAsync();

            var completedLessonIds = await _completionRepository.Where(obj =>
                    obj.StudentId == reply.UserId && lessonIds.Contains(obj.LessonId))
                .Select(obj => obj.LessonId)
                .ToListAsync();

            var grade = await _gradeRepository.SingleOrDefaultAsync(obj =>
                obj.CourseId == courseId && obj.StudentId == reply.UserId);

            var totalLessons = lessonIds.Count;
            var completedLessons = completedLessonIds.Distinct().Count();
            var percent = totalLessons == 0 ? 0 : (int)Math.Round((double)completedLessons / totalLessons * 100);

            response.Success = true;
            response.Message = "Course progress retrieved successfully";
            response.Value = new CourseProgressSummaryDto
            {
                CourseId = courseId,
                StudentId = reply.UserId,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                PercentComplete = percent,
                IsCompleted = totalLessons > 0 && completedLessons >= totalLessons,
                Mark = grade?.Mark,
                MarkGivenAt = grade?.GivenAt,
                CompletedLessonIds = completedLessonIds.Distinct().ToList()
            };

            return response;
        }

        public async Task<ResponseArray<StudentCourseProgressDto>> GetCourseStudentsProgressAsync(long courseId, string teacherEmail)
        {
            var response = new ResponseArray<StudentCourseProgressDto>
            {
                Message = "You don't have such rights"
            };

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

            var teacherReply = await _grpcClient.GetUserInfoForCourse(universityName, teacherEmail);
            if (teacherReply == null || !teacherReply.IsAllowed || (!teacherReply.IsTeacher && !teacherReply.IsDirector))
            {
                response.Message = "Only university teachers or directors can view student progress";
                return response;
            }

            var lessonIds = await _lessonRepository.Where(obj => obj.CourseId == courseId)
                .Select(obj => obj.Id)
                .ToListAsync();

            var totalLessons = lessonIds.Count;

            var completions = await _completionRepository.Where(obj => lessonIds.Contains(obj.LessonId))
                .ToListAsync();

            var grades = await _gradeRepository.Where(obj => obj.CourseId == courseId)
                .ToListAsync();

            var studentIds = completions.Select(obj => obj.StudentId)
                .Concat(grades.Select(obj => obj.StudentId))
                .Distinct()
                .ToList();

            var progressRows = new List<StudentCourseProgressDto>();

            foreach (var studentId in studentIds)
            {
                var studentCompletedLessons = completions
                    .Where(obj => obj.StudentId == studentId)
                    .Select(obj => obj.LessonId)
                    .Distinct()
                    .Count();

                var percent = totalLessons == 0 ? 0 : (int)Math.Round((double)studentCompletedLessons / totalLessons * 100);
                var mark = grades.FirstOrDefault(obj => obj.StudentId == studentId);
                var studentName = await _grpcClient.GetUserName(studentId) ?? "Unknown";

                progressRows.Add(new StudentCourseProgressDto
                {
                    StudentId = studentId,
                    StudentName = studentName,
                    TotalLessons = totalLessons,
                    CompletedLessons = studentCompletedLessons,
                    PercentComplete = percent,
                    IsCompleted = totalLessons > 0 && studentCompletedLessons >= totalLessons,
                    Mark = mark?.Mark,
                    MarkGivenAt = mark?.GivenAt
                });
            }

            response.Success = true;
            response.Message = "Students progress retrieved successfully";
            response.Values = progressRows
                .OrderByDescending(obj => obj.IsCompleted)
                .ThenByDescending(obj => obj.PercentComplete)
                .ThenBy(obj => obj.StudentName)
                .ToList();

            return response;
        }

        public async Task<ResponseMessage> AssignCourseGradeAsync(long courseId, AssignCourseGradeDto gradeDto, string teacherEmail)
        {
            var response = new ResponseMessage
            {
                Message = "You don't have such rights"
            };

            if (gradeDto.Mark < 1 || gradeDto.Mark > 10)
            {
                response.Message = "Mark must be between 1 and 10";
                return response;
            }

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

            var teacherReply = await _grpcClient.GetUserInfoForCourse(universityName, teacherEmail);
            if (teacherReply == null || !teacherReply.IsAllowed || (!teacherReply.IsTeacher && !teacherReply.IsDirector))
            {
                response.Message = "Only university teachers or directors can grade students";
                return response;
            }

            var lessonIds = await _lessonRepository.Where(obj => obj.CourseId == courseId)
                .Select(obj => obj.Id)
                .ToListAsync();

            if (!lessonIds.Any())
            {
                response.Message = "Cannot grade students in an empty course";
                return response;
            }

            var completedLessons = await _completionRepository.Where(obj =>
                    obj.StudentId == gradeDto.StudentId && lessonIds.Contains(obj.LessonId))
                .Select(obj => obj.LessonId)
                .Distinct()
                .CountAsync();

            if (completedLessons < lessonIds.Count)
            {
                response.Message = "Student has not completed all lessons in this course";
                return response;
            }

            var existingGrade = await _gradeRepository.SingleOrDefaultAsync(obj =>
                obj.CourseId == courseId && obj.StudentId == gradeDto.StudentId);

            if (existingGrade == null)
            {
                await _gradeRepository.AddAsync(new StudentCourseGrade
                {
                    CourseId = courseId,
                    StudentId = gradeDto.StudentId,
                    Mark = gradeDto.Mark,
                    GivenByTeacherId = teacherReply.UserId,
                    GivenAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingGrade.Mark = gradeDto.Mark;
                existingGrade.GivenByTeacherId = teacherReply.UserId;
                existingGrade.GivenAt = DateTime.UtcNow;
                await _gradeRepository.UpdateAsync(existingGrade);
            }

            response.Success = true;
            response.Message = "Student grade saved successfully";
            return response;
        }
    }
}
