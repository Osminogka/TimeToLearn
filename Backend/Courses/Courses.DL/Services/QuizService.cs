using Courses.DAL.Dtos;
using Courses.DAL.Models;
using Courses.DAL.SideModels;
using Courses.DL.Grpc;
using Courses.DL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Courses.DL.Services
{
    public class QuizService : IQuizService
    {
        private const string SingleAttemptPolicy = "single";
        private const string ReattemptPolicy = "reattempt";

        private readonly IBaseRepository<Course> _courseRepository;
        private readonly IBaseRepository<Lesson> _lessonRepository;
        private readonly IBaseRepository<QuizQuestion> _quizQuestionRepository;
        private readonly IBaseRepository<QuizOption> _quizOptionRepository;
        private readonly IBaseRepository<QuizAnswer> _quizAnswerRepository;
        private readonly IUserInfoClient _grpcClient;

        public QuizService(
            IBaseRepository<Course> courseRepository,
            IBaseRepository<Lesson> lessonRepository,
            IBaseRepository<QuizQuestion> quizQuestionRepository,
            IBaseRepository<QuizOption> quizOptionRepository,
            IBaseRepository<QuizAnswer> quizAnswerRepository,
            IUserInfoClient grpcClient)
        {
            _courseRepository = courseRepository;
            _lessonRepository = lessonRepository;
            _quizQuestionRepository = quizQuestionRepository;
            _quizOptionRepository = quizOptionRepository;
            _quizAnswerRepository = quizAnswerRepository;
            _grpcClient = grpcClient;
        }

        private static bool IsTeacherLike(UserInfoForCourse info) => info.IsTeacher || info.IsDirector;

        private async Task<(Course? course, string universityName, UserInfoForCourse? userInfo, string? error)> ResolveCourseContextAsync(long courseId, string userEmail)
        {
            var course = await _courseRepository.SingleOrDefaultAsync(obj => obj.Id == courseId);
            if (course == null)
                return (null, string.Empty, null, "Such course doesn't exist");

            var universityName = await _grpcClient.GetUniversityName(course.UniversityId);
            if (string.IsNullOrWhiteSpace(universityName))
                return (null, string.Empty, null, "University not found");

            var userInfo = await _grpcClient.GetUserInfoForCourse(universityName, userEmail);
            if (userInfo == null || !userInfo.IsAllowed)
                return (course, universityName, null, "You don't have such rights");

            return (course, universityName, userInfo, null);
        }

        private async Task<(Lesson? lesson, Course? course, string universityName, UserInfoForCourse? userInfo, string? error)> ResolveLessonContextAsync(long lessonId, string userEmail)
        {
            var lesson = await _lessonRepository.Where(obj => obj.Id == lessonId)
                .Include(obj => obj.Course)
                .FirstOrDefaultAsync();

            if (lesson == null)
                return (null, null, string.Empty, null, "Such lesson doesn't exist");

            var universityName = await _grpcClient.GetUniversityName(lesson.Course.UniversityId);
            if (string.IsNullOrWhiteSpace(universityName))
                return (lesson, lesson.Course, string.Empty, null, "University not found");

            var userInfo = await _grpcClient.GetUserInfoForCourse(universityName, userEmail);
            if (userInfo == null || !userInfo.IsAllowed)
                return (lesson, lesson.Course, universityName, null, "You don't have such rights");

            return (lesson, lesson.Course, universityName, userInfo, null);
        }

        private static (bool valid, string? error, List<CreateQuizOptionDto> options) ValidateQuestionPayload(CreateQuizQuestionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.QuestionText))
                return (false, "Question cannot be empty", new List<CreateQuizOptionDto>());

            var options = dto.Options
                .Where(opt => opt != null && !string.IsNullOrWhiteSpace(opt.OptionText))
                .Select(opt => new CreateQuizOptionDto
                {
                    OptionText = opt.OptionText.Trim(),
                    IsCorrect = opt.IsCorrect
                })
                .ToList();

            if (options.Count < 2)
                return (false, "Provide at least 2 answer options", options);

            if (options.Count(opt => opt.IsCorrect) != 1)
                return (false, "Exactly one option must be marked correct", options);

            return (true, null, options);
        }

        private static string NormalizeAttemptPolicy(string? raw)
        {
            var value = string.IsNullOrWhiteSpace(raw) ? ReattemptPolicy : raw.Trim().ToLowerInvariant();
            return value == SingleAttemptPolicy ? SingleAttemptPolicy : ReattemptPolicy;
        }

        private static QuizQuestionDto MapQuestion(QuizQuestion question, bool includeCorrect)
        {
            return new QuizQuestionDto
            {
                Id = question.Id,
                CourseId = question.CourseId,
                LessonId = question.LessonId,
                IsCourseLevel = !question.LessonId.HasValue,
                QuestionText = question.QuestionText,
                AttemptPolicy = question.AttemptPolicy,
                Options = question.Options
                    .OrderBy(opt => opt.Id)
                    .Select(opt => new QuizOptionDto
                    {
                        Id = opt.Id,
                        OptionText = opt.OptionText,
                        IsCorrect = includeCorrect ? opt.IsCorrect : null
                    })
                    .ToList()
            };
        }

        public async Task<ResponseArray<QuizQuestionDto>> GetLessonQuizQuestionsAsync(long lessonId, string userEmail)
        {
            var response = new ResponseArray<QuizQuestionDto> { Message = "You don't have such rights" };

            var context = await ResolveLessonContextAsync(lessonId, userEmail);
            if (context.error != null)
            {
                response.Message = context.error;
                return response;
            }

            var questions = await _quizQuestionRepository.Where(obj => obj.LessonId == lessonId)
                .Include(obj => obj.Options)
                .OrderBy(obj => obj.Id)
                .ToListAsync();

            var includeCorrect = context.userInfo != null && IsTeacherLike(context.userInfo);

            response.Success = true;
            response.Message = "Lesson quiz questions retrieved successfully";
            response.Values = questions.Select(question => MapQuestion(question, includeCorrect)).ToList();
            return response;
        }

        public async Task<ResponseArray<QuizQuestionDto>> GetCourseQuizQuestionsAsync(long courseId, string userEmail)
        {
            var response = new ResponseArray<QuizQuestionDto> { Message = "You don't have such rights" };

            var context = await ResolveCourseContextAsync(courseId, userEmail);
            if (context.error != null)
            {
                response.Message = context.error;
                return response;
            }

            var questions = await _quizQuestionRepository.Where(obj => obj.CourseId == courseId && obj.LessonId == null)
                .Include(obj => obj.Options)
                .OrderBy(obj => obj.Id)
                .ToListAsync();

            var includeCorrect = context.userInfo != null && IsTeacherLike(context.userInfo);

            response.Success = true;
            response.Message = "Course quiz questions retrieved successfully";
            response.Values = questions.Select(question => MapQuestion(question, includeCorrect)).ToList();
            return response;
        }

        public async Task<ResponseMessage> CreateLessonQuizQuestionAsync(long lessonId, CreateQuizQuestionDto dto, string teacherEmail)
        {
            var response = new ResponseMessage { Message = "You don't have such rights" };

            var context = await ResolveLessonContextAsync(lessonId, teacherEmail);
            if (context.error != null)
            {
                response.Message = context.error;
                return response;
            }

            if (context.userInfo == null || !IsTeacherLike(context.userInfo))
            {
                response.Message = "Only university teachers or directors can create lesson mini quizzes";
                return response;
            }

            var payloadValidation = ValidateQuestionPayload(dto);
            if (!payloadValidation.valid)
            {
                response.Message = payloadValidation.error ?? "Invalid quiz question";
                return response;
            }

            var question = new QuizQuestion
            {
                CourseId = context.course!.Id,
                LessonId = lessonId,
                QuestionText = dto.QuestionText.Trim(),
                AttemptPolicy = NormalizeAttemptPolicy(dto.AttemptPolicy),
                CreatedAt = DateTime.UtcNow,
            };

            await _quizQuestionRepository.AddAsync(question);

            foreach (var option in payloadValidation.options)
            {
                await _quizOptionRepository.AddAsync(new QuizOption
                {
                    QuizQuestionId = question.Id,
                    OptionText = option.OptionText,
                    IsCorrect = option.IsCorrect,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            response.Success = true;
            response.Message = "Lesson mini quiz question created successfully";
            return response;
        }

        public async Task<ResponseMessage> CreateCourseQuizQuestionAsync(long courseId, CreateQuizQuestionDto dto, string teacherEmail)
        {
            var response = new ResponseMessage { Message = "You don't have such rights" };

            var context = await ResolveCourseContextAsync(courseId, teacherEmail);
            if (context.error != null)
            {
                response.Message = context.error;
                return response;
            }

            if (context.userInfo == null || !IsTeacherLike(context.userInfo))
            {
                response.Message = "Only university teachers or directors can create course quizzes";
                return response;
            }

            var payloadValidation = ValidateQuestionPayload(dto);
            if (!payloadValidation.valid)
            {
                response.Message = payloadValidation.error ?? "Invalid quiz question";
                return response;
            }

            var question = new QuizQuestion
            {
                CourseId = courseId,
                LessonId = null,
                QuestionText = dto.QuestionText.Trim(),
                AttemptPolicy = NormalizeAttemptPolicy(dto.AttemptPolicy),
                CreatedAt = DateTime.UtcNow,
            };

            await _quizQuestionRepository.AddAsync(question);

            foreach (var option in payloadValidation.options)
            {
                await _quizOptionRepository.AddAsync(new QuizOption
                {
                    QuizQuestionId = question.Id,
                    OptionText = option.OptionText,
                    IsCorrect = option.IsCorrect,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            response.Success = true;
            response.Message = "Course quiz question created successfully";
            return response;
        }

        private async Task<ResponseMessage> SubmitAnswerAsync(long expectedCourseId, long? expectedLessonId, long questionId, SubmitQuizAnswerDto dto, string userEmail)
        {
            var response = new ResponseMessage { Message = "You don't have such rights" };

            if (dto.SelectedOptionId <= 0)
            {
                response.Message = "Please select an answer option";
                return response;
            }

            UserInfoForCourse? userInfo;
            if (expectedLessonId.HasValue)
            {
                var lessonContext = await ResolveLessonContextAsync(expectedLessonId.Value, userEmail);
                if (lessonContext.error != null)
                {
                    response.Message = lessonContext.error;
                    return response;
                }

                userInfo = lessonContext.userInfo;
            }
            else
            {
                var courseContext = await ResolveCourseContextAsync(expectedCourseId, userEmail);
                if (courseContext.error != null)
                {
                    response.Message = courseContext.error;
                    return response;
                }

                userInfo = courseContext.userInfo;
            }

            if (userInfo == null || IsTeacherLike(userInfo))
            {
                response.Message = "Only students can submit quiz answers";
                return response;
            }

            var question = await _quizQuestionRepository.Where(obj => obj.Id == questionId)
                .Include(obj => obj.Options)
                .FirstOrDefaultAsync();

            if (question == null)
            {
                response.Message = "Quiz question does not exist";
                return response;
            }

            if (question.CourseId != expectedCourseId || question.LessonId != expectedLessonId)
            {
                response.Message = "Question does not belong to this quiz scope";
                return response;
            }

            var selectedOption = question.Options.FirstOrDefault(opt => opt.Id == dto.SelectedOptionId);
            if (selectedOption == null)
            {
                response.Message = "Selected option is invalid";
                return response;
            }

            var existingAnswer = await _quizAnswerRepository.SingleOrDefaultAsync(obj =>
                obj.StudentId == userInfo.UserId && obj.QuizQuestionId == questionId);

            if (existingAnswer == null)
            {
                await _quizAnswerRepository.AddAsync(new QuizAnswer
                {
                    QuizQuestionId = questionId,
                    SelectedOptionId = selectedOption.Id,
                    StudentId = userInfo.UserId,
                    IsCorrect = selectedOption.IsCorrect,
                    AnsweredAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            else
            {
                if (string.Equals(question.AttemptPolicy, SingleAttemptPolicy, StringComparison.OrdinalIgnoreCase))
                {
                    response.Message = "This question is locked to a single attempt";
                    return response;
                }

                existingAnswer.SelectedOptionId = selectedOption.Id;
                existingAnswer.IsCorrect = selectedOption.IsCorrect;
                existingAnswer.AnsweredAt = DateTime.UtcNow;
                await _quizAnswerRepository.UpdateAsync(existingAnswer);
            }

            response.Success = true;
            response.Message = selectedOption.IsCorrect ? "Correct answer submitted" : "Answer submitted";
            return response;
        }

        public async Task<ResponseMessage> SubmitLessonQuizAnswerAsync(long lessonId, long questionId, SubmitQuizAnswerDto dto, string userEmail)
        {
            var lesson = await _lessonRepository.SingleOrDefaultAsync(obj => obj.Id == lessonId);
            if (lesson == null)
            {
                return new ResponseMessage
                {
                    Message = "Such lesson doesn't exist"
                };
            }

            return await SubmitAnswerAsync(lesson.CourseId, lessonId, questionId, dto, userEmail);
        }

        public async Task<ResponseMessage> SubmitCourseQuizAnswerAsync(long courseId, long questionId, SubmitQuizAnswerDto dto, string userEmail)
            => await SubmitAnswerAsync(courseId, null, questionId, dto, userEmail);

        public async Task<ResponseArray<StudentQuizAnswerDto>> GetMyLessonQuizAnswersAsync(long lessonId, string userEmail)
        {
            var response = new ResponseArray<StudentQuizAnswerDto> { Message = "You don't have such rights" };

            var context = await ResolveLessonContextAsync(lessonId, userEmail);
            if (context.error != null)
            {
                response.Message = context.error;
                return response;
            }

            if (context.userInfo == null || IsTeacherLike(context.userInfo))
            {
                response.Message = "Only students can view own quiz answers";
                return response;
            }

            var answers = await _quizAnswerRepository.Where(obj => obj.StudentId == context.userInfo.UserId && obj.QuizQuestion.LessonId == lessonId)
                .Include(obj => obj.QuizQuestion)
                .Include(obj => obj.SelectedOption)
                .OrderByDescending(obj => obj.AnsweredAt)
                .ToListAsync();

            response.Success = true;
            response.Message = "Your lesson quiz answers retrieved successfully";
            response.Values = answers.Select(obj => new StudentQuizAnswerDto
            {
                QuestionId = obj.QuizQuestionId,
                QuestionText = obj.QuizQuestion.QuestionText,
                SelectedOptionId = obj.SelectedOptionId,
                SelectedOptionText = obj.SelectedOption.OptionText,
                IsCorrect = obj.IsCorrect,
                AnsweredAt = obj.AnsweredAt
            }).ToList();

            return response;
        }

        public async Task<ResponseArray<StudentQuizAnswerDto>> GetMyCourseQuizAnswersAsync(long courseId, string userEmail)
        {
            var response = new ResponseArray<StudentQuizAnswerDto> { Message = "You don't have such rights" };

            var context = await ResolveCourseContextAsync(courseId, userEmail);
            if (context.error != null)
            {
                response.Message = context.error;
                return response;
            }

            if (context.userInfo == null || IsTeacherLike(context.userInfo))
            {
                response.Message = "Only students can view own quiz answers";
                return response;
            }

            var answers = await _quizAnswerRepository.Where(obj => obj.StudentId == context.userInfo.UserId && obj.QuizQuestion.CourseId == courseId && obj.QuizQuestion.LessonId == null)
                .Include(obj => obj.QuizQuestion)
                .Include(obj => obj.SelectedOption)
                .OrderByDescending(obj => obj.AnsweredAt)
                .ToListAsync();

            response.Success = true;
            response.Message = "Your course quiz answers retrieved successfully";
            response.Values = answers.Select(obj => new StudentQuizAnswerDto
            {
                QuestionId = obj.QuizQuestionId,
                QuestionText = obj.QuizQuestion.QuestionText,
                SelectedOptionId = obj.SelectedOptionId,
                SelectedOptionText = obj.SelectedOption.OptionText,
                IsCorrect = obj.IsCorrect,
                AnsweredAt = obj.AnsweredAt
            }).ToList();

            return response;
        }

        private async Task<ResponseArray<QuizAnswerReviewDto>> GetAnswersForTeacherAsync(long courseId, long? lessonId, string teacherEmail)
        {
            var response = new ResponseArray<QuizAnswerReviewDto> { Message = "You don't have such rights" };

            var courseContext = await ResolveCourseContextAsync(courseId, teacherEmail);
            if (courseContext.error != null)
            {
                response.Message = courseContext.error;
                return response;
            }

            if (courseContext.userInfo == null || !IsTeacherLike(courseContext.userInfo))
            {
                response.Message = "Only university teachers or directors can review quiz answers";
                return response;
            }

            var answers = await _quizAnswerRepository.Where(obj =>
                    obj.QuizQuestion.CourseId == courseId && obj.QuizQuestion.LessonId == lessonId)
                .Include(obj => obj.QuizQuestion)
                .Include(obj => obj.SelectedOption)
                .Include(obj => obj.QuizQuestion.Options)
                .OrderByDescending(obj => obj.AnsweredAt)
                .ToListAsync();

            var studentIds = answers.Select(obj => obj.StudentId).Distinct().ToList();
            var nameEntries = await Task.WhenAll(studentIds.Select(async id => (id, name: await _grpcClient.GetUserName(id))));
            var names = nameEntries.ToDictionary(obj => obj.id, obj => obj.name ?? "Unknown");

            response.Success = true;
            response.Message = "Quiz answers retrieved successfully";
            response.Values = answers.Select(obj => new QuizAnswerReviewDto
            {
                StudentId = obj.StudentId,
                StudentName = names.TryGetValue(obj.StudentId, out var name) ? name : "Unknown",
                QuestionId = obj.QuizQuestionId,
                QuestionText = obj.QuizQuestion.QuestionText,
                SelectedOptionText = obj.SelectedOption.OptionText,
                CorrectOptionText = obj.QuizQuestion.Options.FirstOrDefault(opt => opt.IsCorrect)?.OptionText ?? string.Empty,
                IsCorrect = obj.IsCorrect,
                AnsweredAt = obj.AnsweredAt,
            }).ToList();

            return response;
        }

        public async Task<ResponseArray<QuizAnswerReviewDto>> GetLessonQuizAnswersForTeacherAsync(long lessonId, string teacherEmail)
        {
            var lessonContext = await ResolveLessonContextAsync(lessonId, teacherEmail);
            if (lessonContext.error != null)
            {
                return new ResponseArray<QuizAnswerReviewDto> { Message = lessonContext.error };
            }

            return await GetAnswersForTeacherAsync(lessonContext.course!.Id, lessonId, teacherEmail);
        }

        public async Task<ResponseArray<QuizAnswerReviewDto>> GetCourseQuizAnswersForTeacherAsync(long courseId, string teacherEmail)
            => await GetAnswersForTeacherAsync(courseId, null, teacherEmail);
    }
}
