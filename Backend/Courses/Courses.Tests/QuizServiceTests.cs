using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Courses.DAL.Context;
using Courses.DAL.Dtos;
using Courses.DAL.Models;
using Courses.DAL.SideModels;
using Courses.DL.Grpc;
using Courses.DL.Repositories;
using Courses.DL.Services;

namespace Courses.Tests;

public class QuizServiceTests
{
    private readonly IBaseRepository<Course> _courseRepo;
    private readonly IBaseRepository<Lesson> _lessonRepo;
    private readonly IBaseRepository<QuizQuestion> _quizQuestionRepo;
    private readonly IBaseRepository<QuizOption> _quizOptionRepo;
    private readonly IBaseRepository<QuizAnswer> _quizAnswerRepo;
    private readonly Mock<IUserInfoClient> _grpcMock;
    private readonly QuizService _service;

    private const string TeacherEmail = "teacher@test.com";
    private const string StudentEmail = "student@test.com";
    private const string UniversityName = "TestUniversity";
    private const long UniversityId = 1;
    private const long TeacherId = 10;
    private const long StudentId = 20;

    public QuizServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("TestDbQuizService"));
        services.AddTransient<IBaseRepository<Course>, BaseRepository<Course>>();
        services.AddTransient<IBaseRepository<Lesson>, BaseRepository<Lesson>>();
        services.AddTransient<IBaseRepository<QuizQuestion>, BaseRepository<QuizQuestion>>();
        services.AddTransient<IBaseRepository<QuizOption>, BaseRepository<QuizOption>>();
        services.AddTransient<IBaseRepository<QuizAnswer>, BaseRepository<QuizAnswer>>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        _courseRepo = scoped.GetRequiredService<IBaseRepository<Course>>();
        _lessonRepo = scoped.GetRequiredService<IBaseRepository<Lesson>>();
        _quizQuestionRepo = scoped.GetRequiredService<IBaseRepository<QuizQuestion>>();
        _quizOptionRepo = scoped.GetRequiredService<IBaseRepository<QuizOption>>();
        _quizAnswerRepo = scoped.GetRequiredService<IBaseRepository<QuizAnswer>>();

        var context = scoped.GetRequiredService<DataContext>();
        context.Database.EnsureDeleted();

        context.Add(new Course
        {
            Id = 1,
            Title = "CS101",
            Description = "Intro",
            TeacherId = TeacherId,
            UniversityId = UniversityId,
            CreatedAt = DateTime.UtcNow
        });
        context.Add(new Lesson
        {
            Id = 1,
            CourseId = 1,
            Title = "Lesson 1",
            Content = "Content",
            OrderNumber = 1,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        _grpcMock = new Mock<IUserInfoClient>();
        _service = new QuizService(_courseRepo, _lessonRepo, _quizQuestionRepo, _quizOptionRepo, _quizAnswerRepo, _grpcMock.Object);
    }

    private void SetupTeacher(string email = TeacherEmail)
    {
        _grpcMock.Setup(m => m.GetUniversityName(UniversityId)).ReturnsAsync(UniversityName);
        _grpcMock.Setup(m => m.GetUserInfoForCourse(UniversityName, email)).ReturnsAsync(new UserInfoForCourse
        {
            UserId = TeacherId,
            UniversityId = UniversityId,
            IsAllowed = true,
            IsTeacher = true
        });
    }

    private void SetupStudent(string email = StudentEmail)
    {
        _grpcMock.Setup(m => m.GetUniversityName(UniversityId)).ReturnsAsync(UniversityName);
        _grpcMock.Setup(m => m.GetUserInfoForCourse(UniversityName, email)).ReturnsAsync(new UserInfoForCourse
        {
            UserId = StudentId,
            UniversityId = UniversityId,
            IsAllowed = true,
            IsTeacher = false,
            IsDirector = false
        });
    }

    // Seeds a quiz question with two options and returns (questionId, correctOptionId, wrongOptionId).
    private async Task<(long qId, long correctOptId, long wrongOptId)> SeedQuestionWithOptionsAsync(
        string attemptPolicy = "reattempt")
    {
        var question = new QuizQuestion
        {
            CourseId = 1,
            LessonId = 1,
            QuestionText = "What is 2+2?",
            AttemptPolicy = attemptPolicy,
            CreatedAt = DateTime.UtcNow
        };
        await _quizQuestionRepo.AddAsync(question);

        var correctOpt = new QuizOption { QuizQuestionId = question.Id, OptionText = "4", IsCorrect = true, CreatedAt = DateTime.UtcNow };
        var wrongOpt = new QuizOption { QuizQuestionId = question.Id, OptionText = "5", IsCorrect = false, CreatedAt = DateTime.UtcNow };
        await _quizOptionRepo.AddAsync(correctOpt);
        await _quizOptionRepo.AddAsync(wrongOpt);

        return (question.Id, correctOpt.Id, wrongOpt.Id);
    }

    [Fact]
    public async Task CreateLessonQuizQuestion_Teacher_CreatesQuestionAndOptions()
    {
        SetupTeacher();
        var dto = new CreateQuizQuestionDto
        {
            QuestionText = "What is 1+1?",
            AttemptPolicy = "reattempt",
            Options = new List<CreateQuizOptionDto>
            {
                new CreateQuizOptionDto { OptionText = "2", IsCorrect = true },
                new CreateQuizOptionDto { OptionText = "3", IsCorrect = false }
            }
        };

        var result = await _service.CreateLessonQuizQuestionAsync(1, dto, TeacherEmail);

        Assert.True(result.Success);
        var saved = await _quizQuestionRepo.SingleOrDefaultAsync(q => q.QuestionText == "What is 1+1?");
        Assert.NotNull(saved);
        Assert.Equal(1, saved.LessonId);
    }

    [Fact]
    public async Task CreateLessonQuizQuestion_TooFewOptions_Fails()
    {
        SetupTeacher();
        var dto = new CreateQuizQuestionDto
        {
            QuestionText = "Solo question?",
            Options = new List<CreateQuizOptionDto>
            {
                new CreateQuizOptionDto { OptionText = "Only option", IsCorrect = true }
            }
        };

        var result = await _service.CreateLessonQuizQuestionAsync(1, dto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Provide at least 2 answer options", result.Message);
    }

    [Fact]
    public async Task CreateLessonQuizQuestion_MultipleCorrectOptions_Fails()
    {
        SetupTeacher();
        var dto = new CreateQuizQuestionDto
        {
            QuestionText = "Ambiguous?",
            Options = new List<CreateQuizOptionDto>
            {
                new CreateQuizOptionDto { OptionText = "A", IsCorrect = true },
                new CreateQuizOptionDto { OptionText = "B", IsCorrect = true }  // two correct — invalid
            }
        };

        var result = await _service.CreateLessonQuizQuestionAsync(1, dto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Exactly one option must be marked correct", result.Message);
    }

    [Fact]
    public async Task SubmitLessonQuiz_Teacher_Blocked()
    {
        var (qId, correctOptId, _) = await SeedQuestionWithOptionsAsync();
        SetupTeacher();
        var dto = new SubmitQuizDto
        {
            Answers = new List<SubmitQuizQuestionAnswerDto>
            {
                new SubmitQuizQuestionAnswerDto { QuestionId = qId, SelectedOptionId = correctOptId }
            }
        };

        var result = await _service.SubmitLessonQuizAsync(1, dto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Only students can submit quiz answers", result.Message);
    }

    [Fact]
    public async Task SubmitLessonQuiz_Student_RecordsAnswerWithCorrectness()
    {
        var (qId, correctOptId, _) = await SeedQuestionWithOptionsAsync();
        SetupStudent();
        var dto = new SubmitQuizDto
        {
            Answers = new List<SubmitQuizQuestionAnswerDto>
            {
                new SubmitQuizQuestionAnswerDto { QuestionId = qId, SelectedOptionId = correctOptId }
            }
        };

        var result = await _service.SubmitLessonQuizAsync(1, dto, StudentEmail);

        Assert.True(result.Success);
        var answer = await _quizAnswerRepo.SingleOrDefaultAsync(a => a.StudentId == StudentId && a.QuizQuestionId == qId);
        Assert.NotNull(answer);
        Assert.True(answer.IsCorrect);
    }

    [Fact]
    public async Task SubmitLessonQuiz_SingleAttemptPolicy_BlocksRetake()
    {
        var (qId, correctOptId, _) = await SeedQuestionWithOptionsAsync(attemptPolicy: "single");
        SetupStudent();
        var dto = new SubmitQuizDto
        {
            Answers = new List<SubmitQuizQuestionAnswerDto>
            {
                new SubmitQuizQuestionAnswerDto { QuestionId = qId, SelectedOptionId = correctOptId }
            }
        };

        // First submission must succeed
        var firstResult = await _service.SubmitLessonQuizAsync(1, dto, StudentEmail);
        Assert.True(firstResult.Success);

        // Second submission must be blocked
        var secondResult = await _service.SubmitLessonQuizAsync(1, dto, StudentEmail);
        Assert.False(secondResult.Success);
        Assert.Equal("This quiz is locked to a single attempt", secondResult.Message);
    }
}
