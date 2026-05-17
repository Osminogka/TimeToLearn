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

public class LessonServiceTests
{
    private readonly IBaseRepository<Course> _courseRepo;
    private readonly IBaseRepository<Lesson> _lessonRepo;
    private readonly IBaseRepository<LessonResource> _resourceRepo;
    private readonly IBaseRepository<StudentLessonCompletion> _completionRepo;
    private readonly IBaseRepository<QuizQuestion> _quizQuestionRepo;
    private readonly IBaseRepository<QuizAnswer> _quizAnswerRepo;
    private readonly Mock<IUserInfoClient> _grpcMock;
    private readonly Mock<IMarkdownService> _markdownMock;
    private readonly LessonService _service;

    private const string TeacherEmail = "teacher@test.com";
    private const string StudentEmail = "student@test.com";
    private const string UniversityName = "TestUniversity";
    private const long UniversityId = 1;
    private const long TeacherId = 10;
    private const long StudentId = 20;

    public LessonServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("TestDbLessonService"));
        services.AddTransient<IBaseRepository<Course>, BaseRepository<Course>>();
        services.AddTransient<IBaseRepository<Lesson>, BaseRepository<Lesson>>();
        services.AddTransient<IBaseRepository<LessonResource>, BaseRepository<LessonResource>>();
        services.AddTransient<IBaseRepository<StudentLessonCompletion>, BaseRepository<StudentLessonCompletion>>();
        services.AddTransient<IBaseRepository<QuizQuestion>, BaseRepository<QuizQuestion>>();
        services.AddTransient<IBaseRepository<QuizAnswer>, BaseRepository<QuizAnswer>>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        _courseRepo = scoped.GetRequiredService<IBaseRepository<Course>>();
        _lessonRepo = scoped.GetRequiredService<IBaseRepository<Lesson>>();
        _resourceRepo = scoped.GetRequiredService<IBaseRepository<LessonResource>>();
        _completionRepo = scoped.GetRequiredService<IBaseRepository<StudentLessonCompletion>>();
        _quizQuestionRepo = scoped.GetRequiredService<IBaseRepository<QuizQuestion>>();
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
        context.Add(new Lesson { Id = 1, CourseId = 1, Title = "Lesson 1", Content = "Content A", OrderNumber = 1, CreatedAt = DateTime.UtcNow });
        context.Add(new Lesson { Id = 2, CourseId = 1, Title = "Lesson 2", Content = "Content B", OrderNumber = 2, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();

        _grpcMock = new Mock<IUserInfoClient>();
        _markdownMock = new Mock<IMarkdownService>();
        _markdownMock.Setup(m => m.ConvertToHtml(It.IsAny<string>())).Returns("<p>html</p>");

        _service = new LessonService(
            _lessonRepo, _resourceRepo, _courseRepo, _completionRepo,
            _quizQuestionRepo, _quizAnswerRepo, _grpcMock.Object, _markdownMock.Object);
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

    [Fact]
    public async Task CreateLesson_Teacher_CreatesSuccessfully()
    {
        SetupTeacher();
        var dto = new CreateLessonDto
        {
            CourseId = 1,
            Title = "New Lesson",
            Content = "Lesson content",
            IsMarkdown = true,
            OrderNumber = 3
        };

        var result = await _service.CreateLessonAsync(dto, TeacherEmail);

        Assert.True(result.Success);
        var saved = await _lessonRepo.SingleOrDefaultAsync(l => l.Title == "New Lesson");
        Assert.NotNull(saved);
        Assert.Equal(1, saved.CourseId);
        Assert.Equal(3, saved.OrderNumber);
    }

    [Fact]
    public async Task CreateLesson_EmptyTitle_Fails()
    {
        var dto = new CreateLessonDto { CourseId = 1, Title = "", Content = "Content", OrderNumber = 1 };

        var result = await _service.CreateLessonAsync(dto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Lesson title cannot be empty", result.Message);
        _grpcMock.Verify(m => m.GetUserInfoForCourse(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateLesson_EmptyContent_Fails()
    {
        var dto = new CreateLessonDto { CourseId = 1, Title = "Valid Title", Content = "  ", OrderNumber = 1 };

        var result = await _service.CreateLessonAsync(dto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Lesson content cannot be empty", result.Message);
        _grpcMock.Verify(m => m.GetUserInfoForCourse(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateLesson_Student_Fails()
    {
        SetupStudent();
        var dto = new CreateLessonDto { CourseId = 1, Title = "Lesson", Content = "Content", OrderNumber = 1 };

        var result = await _service.CreateLessonAsync(dto, StudentEmail);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReorderLessons_CountMismatch_Fails()
    {
        // Provide 3 items for a course that only has 2 lessons
        SetupTeacher();
        var dto = new ReorderLessonsDto
        {
            CourseId = 1,
            Items = new List<LessonOrderItemDto>
            {
                new LessonOrderItemDto { LessonId = 1, OrderNumber = 2 },
                new LessonOrderItemDto { LessonId = 2, OrderNumber = 1 },
                new LessonOrderItemDto { LessonId = 99, OrderNumber = 3 }  // doesn't exist in course
            }
        };

        var result = await _service.ReorderLessonsAsync(dto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("One or more lessons do not belong to this course", result.Message);
    }
}
