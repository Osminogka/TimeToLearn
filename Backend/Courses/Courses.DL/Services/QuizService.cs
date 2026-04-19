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

        private static string ResolveQuizAttemptPolicy(IEnumerable<QuizQuestion> questions)
        {
            return questions.Any(obj => string.Equals(obj.AttemptPolicy, SingleAttemptPolicy, StringComparison.OrdinalIgnoreCase))
                ? SingleAttemptPolicy
                : ReattemptPolicy;
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

        private async Task RemoveAnswersForQuestionsAsync(List<long> questionIds)
        {
            if (!questionIds.Any())
            {
                return;
            }

            var answers = await _quizAnswerRepository.Where(obj => questionIds.Contains(obj.QuizQuestionId))
                .ToListAsync();

            if (answers.Any())
            {
                await _quizAnswerRepository.DeleteRangeAsync(answers);
            }
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

        private async Task<ResponseMessage> UpsertQuizAsync(long expectedCourseId, long? expectedLessonId, IEnumerable<CreateQuizQuestionDto>? questions, string attemptPolicy, string teacherEmail)
        {
            var response = new ResponseMessage { Message = "You don't have such rights" };

            if (expectedLessonId.HasValue)
            {
                var lessonContext = await ResolveLessonContextAsync(expectedLessonId.Value, teacherEmail);
                if (lessonContext.error != null)
                {
                    response.Message = lessonContext.error;
                    return response;
                }

                if (lessonContext.userInfo == null || !IsTeacherLike(lessonContext.userInfo))
                {
                    response.Message = "Only university teachers or directors can edit lesson mini quizzes";
                    return response;
                }
            }
            else
            {
                var courseContext = await ResolveCourseContextAsync(expectedCourseId, teacherEmail);
                if (courseContext.error != null)
                {
                    response.Message = courseContext.error;
                    return response;
                }

                if (courseContext.userInfo == null || !IsTeacherLike(courseContext.userInfo))
                {
                    response.Message = "Only university teachers or directors can edit course quizzes";
                    return response;
                }
            }

            var sourceQuestions = (questions ?? Array.Empty<CreateQuizQuestionDto>()).ToList();
            var payloadQuestions = new List<(string QuestionText, List<CreateQuizOptionDto> Options)>();
            var normalizedQuizAttemptPolicy = NormalizeAttemptPolicy(attemptPolicy);

            foreach (var item in sourceQuestions)
            {
                var validated = ValidateQuestionPayload(item);
                if (!validated.valid)
                {
                    response.Message = validated.error ?? "Invalid quiz question";
                    return response;
                }

                payloadQuestions.Add((
                    QuestionText: item.QuestionText.Trim(),
                    Options: validated.options
                ));
            }

            var existingQuestions = await _quizQuestionRepository.Where(obj =>
                    obj.CourseId == expectedCourseId && obj.LessonId == expectedLessonId)
                .Include(obj => obj.Options)
                .ToListAsync();

            if (existingQuestions.Any())
            {
                var existingQuestionIds = existingQuestions.Select(obj => obj.Id).ToList();
                await RemoveAnswersForQuestionsAsync(existingQuestionIds);
                await _quizQuestionRepository.DeleteRangeAsync(existingQuestions);
            }

            foreach (var item in payloadQuestions)
            {
                var question = new QuizQuestion
                {
                    CourseId = expectedCourseId,
                    LessonId = expectedLessonId,
                    QuestionText = item.QuestionText,
                    AttemptPolicy = normalizedQuizAttemptPolicy,
                    CreatedAt = DateTime.UtcNow,
                };

                await _quizQuestionRepository.AddAsync(question);

                foreach (var option in item.Options)
                {
                    await _quizOptionRepository.AddAsync(new QuizOption
                    {
                        QuizQuestionId = question.Id,
                        OptionText = option.OptionText,
                        IsCorrect = option.IsCorrect,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
            }

            response.Success = true;
            response.Message = payloadQuestions.Any() ? "Quiz saved successfully" : "Quiz cleared successfully";
            return response;
        }

        public async Task<ResponseMessage> UpsertLessonQuizAsync(long lessonId, UpsertQuizDto dto, string teacherEmail)
        {
            var lesson = await _lessonRepository.SingleOrDefaultAsync(obj => obj.Id == lessonId);
            if (lesson == null)
            {
                return new ResponseMessage
                {
                    Message = "Such lesson doesn't exist"
                };
            }

            return await UpsertQuizAsync(lesson.CourseId, lessonId, dto.Questions, dto.AttemptPolicy, teacherEmail);
        }

        public async Task<ResponseMessage> UpsertCourseQuizAsync(long courseId, UpsertQuizDto dto, string teacherEmail)
            => await UpsertQuizAsync(courseId, null, dto.Questions, dto.AttemptPolicy, teacherEmail);

        private async Task<ResponseMessage> UpdateQuestionAsync(long expectedCourseId, long? expectedLessonId, long questionId, CreateQuizQuestionDto dto, string teacherEmail)
        {
            var response = new ResponseMessage { Message = "You don't have such rights" };

            if (expectedLessonId.HasValue)
            {
                var lessonContext = await ResolveLessonContextAsync(expectedLessonId.Value, teacherEmail);
                if (lessonContext.error != null)
                {
                    response.Message = lessonContext.error;
                    return response;
                }

                if (lessonContext.userInfo == null || !IsTeacherLike(lessonContext.userInfo))
                {
                    response.Message = "Only university teachers or directors can edit lesson mini quizzes";
                    return response;
                }
            }
            else
            {
                var courseContext = await ResolveCourseContextAsync(expectedCourseId, teacherEmail);
                if (courseContext.error != null)
                {
                    response.Message = courseContext.error;
                    return response;
                }

                if (courseContext.userInfo == null || !IsTeacherLike(courseContext.userInfo))
                {
                    response.Message = "Only university teachers or directors can edit course quizzes";
                    return response;
                }
            }

            var payloadValidation = ValidateQuestionPayload(dto);
            if (!payloadValidation.valid)
            {
                response.Message = payloadValidation.error ?? "Invalid quiz question";
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

            question.QuestionText = dto.QuestionText.Trim();
            question.AttemptPolicy = NormalizeAttemptPolicy(dto.AttemptPolicy);
            question.UpdatedAt = DateTime.UtcNow;
            await _quizQuestionRepository.UpdateAsync(question);

            await RemoveAnswersForQuestionsAsync(new List<long> { question.Id });

            if (question.Options.Any())
            {
                await _quizOptionRepository.DeleteRangeAsync(question.Options.ToList());
            }

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
            response.Message = "Quiz question updated successfully";
            return response;
        }

        public async Task<ResponseMessage> UpdateLessonQuizQuestionAsync(long lessonId, long questionId, CreateQuizQuestionDto dto, string teacherEmail)
        {
            var lesson = await _lessonRepository.SingleOrDefaultAsync(obj => obj.Id == lessonId);
            if (lesson == null)
            {
                return new ResponseMessage
                {
                    Message = "Such lesson doesn't exist"
                };
            }

            return await UpdateQuestionAsync(lesson.CourseId, lessonId, questionId, dto, teacherEmail);
        }

        public async Task<ResponseMessage> UpdateCourseQuizQuestionAsync(long courseId, long questionId, CreateQuizQuestionDto dto, string teacherEmail)
            => await UpdateQuestionAsync(courseId, null, questionId, dto, teacherEmail);

        private async Task<ResponseMessage> SubmitQuizAsync(long expectedCourseId, long? expectedLessonId, SubmitQuizDto dto, string userEmail)
        {
            var response = new ResponseMessage { Message = "You don't have such rights" };

            var submittedAnswers = (dto.Answers ?? Array.Empty<SubmitQuizQuestionAnswerDto>()).ToList();

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

            var questions = await _quizQuestionRepository.Where(obj =>
                    obj.CourseId == expectedCourseId && obj.LessonId == expectedLessonId)
                .Include(obj => obj.Options)
                .ToListAsync();

            if (!questions.Any())
            {
                response.Message = "Quiz has no questions";
                return response;
            }

            if (submittedAnswers.Count != questions.Count)
            {
                response.Message = "Please answer all quiz questions before submitting";
                return response;
            }

            if (submittedAnswers.Any(obj => obj.QuestionId <= 0 || obj.SelectedOptionId <= 0))
            {
                response.Message = "Please select an answer for each question";
                return response;
            }

            var duplicateQuestionIds = submittedAnswers
                .GroupBy(obj => obj.QuestionId)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateQuestionIds.Any())
            {
                response.Message = "Each question must be answered exactly once";
                return response;
            }

            var answersByQuestionId = submittedAnswers.ToDictionary(obj => obj.QuestionId, obj => obj.SelectedOptionId);
            var quizQuestionIds = questions.Select(obj => obj.Id).ToHashSet();

            if (answersByQuestionId.Keys.Any(questionId => !quizQuestionIds.Contains(questionId)))
            {
                response.Message = "Submitted answers contain invalid question ids";
                return response;
            }

            var quizAttemptPolicy = ResolveQuizAttemptPolicy(questions);

            var existingAnswers = await _quizAnswerRepository.Where(obj =>
                    obj.StudentId == userInfo.UserId && quizQuestionIds.Contains(obj.QuizQuestionId))
                .ToListAsync();

            if (string.Equals(quizAttemptPolicy, SingleAttemptPolicy, StringComparison.OrdinalIgnoreCase) && existingAnswers.Any())
            {
                response.Message = "This quiz is locked to a single attempt";
                return response;
            }

            foreach (var question in questions)
            {
                if (!answersByQuestionId.TryGetValue(question.Id, out var selectedOptionId))
                {
                    response.Message = "Please answer all quiz questions before submitting";
                    return response;
                }

                var selectedOption = question.Options.FirstOrDefault(opt => opt.Id == selectedOptionId);
                if (selectedOption == null)
                {
                    response.Message = "Submitted answers contain invalid options";
                    return response;
                }

                var existingAnswer = existingAnswers.FirstOrDefault(obj => obj.QuizQuestionId == question.Id);
                if (existingAnswer == null)
                {
                    await _quizAnswerRepository.AddAsync(new QuizAnswer
                    {
                        QuizQuestionId = question.Id,
                        SelectedOptionId = selectedOption.Id,
                        StudentId = userInfo.UserId,
                        IsCorrect = selectedOption.IsCorrect,
                        AnsweredAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
                else
                {
                    existingAnswer.SelectedOptionId = selectedOption.Id;
                    existingAnswer.IsCorrect = selectedOption.IsCorrect;
                    existingAnswer.AnsweredAt = DateTime.UtcNow;
                    await _quizAnswerRepository.UpdateAsync(existingAnswer);
                }
            }

            response.Success = true;
            response.Message = string.Equals(quizAttemptPolicy, ReattemptPolicy, StringComparison.OrdinalIgnoreCase) && existingAnswers.Any()
                ? "Quiz answers updated"
                : "Quiz answers submitted";
            return response;
        }

        public async Task<ResponseMessage> SubmitLessonQuizAsync(long lessonId, SubmitQuizDto dto, string userEmail)
        {
            var lesson = await _lessonRepository.SingleOrDefaultAsync(obj => obj.Id == lessonId);
            if (lesson == null)
            {
                return new ResponseMessage
                {
                    Message = "Such lesson doesn't exist"
                };
            }

            return await SubmitQuizAsync(lesson.CourseId, lessonId, dto, userEmail);
        }

        public async Task<ResponseMessage> SubmitCourseQuizAsync(long courseId, SubmitQuizDto dto, string userEmail)
            => await SubmitQuizAsync(courseId, null, dto, userEmail);

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
