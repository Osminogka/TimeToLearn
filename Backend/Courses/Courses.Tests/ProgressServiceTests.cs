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

public class ProgressServiceTests
{
    private readonly IBaseRepository<Course> _courseRepo;
    private readonly IBaseRepository<Lesson> _lessonRepo;
    private readonly IBaseRepository<StudentLessonCompletion> _completionRepo;
    private readonly IBaseRepository<StudentCourseGrade> _gradeRepo;
    private readonly IBaseRepository<QuizQuestion> _quizQuestionRepo;
    private readonly IBaseRepository<QuizAnswer> _quizAnswerRepo;
    private readonly Mock<IUserInfoClient> _grpcMock;
    private readonly ProgressService _service;

    private const string TeacherEmail = "teacher@test.com";
    private const string StudentEmail = "student@test.com";
    private const string UniversityName = "TestUniversity";
    private const long UniversityId = 1;
    private const long TeacherId = 10;
    private const long StudentId = 20;

    public ProgressServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("TestDbProgressService"));
        services.AddTransient<IBaseRepository<Course>, BaseRepository<Course>>();
        services.AddTransient<IBaseRepository<Lesson>, BaseRepository<Lesson>>();
        services.AddTransient<IBaseRepository<StudentLessonCompletion>, BaseRepository<StudentLessonCompletion>>();
        services.AddTransient<IBaseRepository<StudentCourseGrade>, BaseRepository<StudentCourseGrade>>();
        services.AddTransient<IBaseRepository<QuizQuestion>, BaseRepository<QuizQuestion>>();
        services.AddTransient<IBaseRepository<QuizAnswer>, BaseRepository<QuizAnswer>>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        _courseRepo = scoped.GetRequiredService<IBaseRepository<Course>>();
        _lessonRepo = scoped.GetRequiredService<IBaseRepository<Lesson>>();
        _completionRepo = scoped.GetRequiredService<IBaseRepository<StudentLessonCompletion>>();
        _gradeRepo = scoped.GetRequiredService<IBaseRepository<StudentCourseGrade>>();
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
        _service = new ProgressService(
            _courseRepo, _lessonRepo, _completionRepo, _gradeRepo,
            _quizQuestionRepo, _quizAnswerRepo, _grpcMock.Object);
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

    [Fact]
    public async Task CompleteLesson_Student_RecordsCompletion()
    {
        SetupStudent();

        var result = await _service.CompleteLessonAsync(1, StudentEmail);

        Assert.True(result.Success);
        var completion = await _completionRepo.SingleOrDefaultAsync(c => c.StudentId == StudentId && c.LessonId == 1);
        Assert.NotNull(completion);
    }

    [Fact]
    public async Task CompleteLesson_Teacher_Blocked()
    {
        SetupTeacher();

        var result = await _service.CompleteLessonAsync(1, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Only students can complete lessons", result.Message);
        var completion = await _completionRepo.SingleOrDefaultAsync(c => c.LessonId == 1);
        Assert.Null(completion);
    }

    [Fact]
    public async Task CompleteLesson_AlreadyCompleted_ReturnsSuccessIdempotently()
    {
        // Pre-seed a completion so the lesson is already done
        await _completionRepo.AddAsync(new StudentLessonCompletion
        {
            StudentId = StudentId,
            LessonId = 1,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        SetupStudent();

        var result = await _service.CompleteLessonAsync(1, StudentEmail);

        Assert.True(result.Success);
        Assert.Equal("Lesson already completed", result.Message);
    }

    [Fact]
    public async Task AssignGrade_MarkOutOfRange_Fails()
    {
        // Mark 0 is below the allowed 1–10 range
        var gradeDto = new AssignCourseGradeDto { StudentId = StudentId, Mark = 0 };

        var result = await _service.AssignCourseGradeAsync(1, gradeDto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Mark must be between 1 and 10", result.Message);
        _grpcMock.Verify(m => m.GetUserInfoForCourse(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AssignGrade_StudentMissingLessonCompletions_Fails()
    {
        // Course has 1 lesson, student has completed 0 — grading must be blocked
        SetupTeacher();
        var gradeDto = new AssignCourseGradeDto { StudentId = StudentId, Mark = 8 };

        var result = await _service.AssignCourseGradeAsync(1, gradeDto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Student has not completed all lessons in this course", result.Message);
    }

    [Fact]
    public async Task AssignGrade_AllLessonsCompleted_SavesGrade()
    {
        // Student completes the only lesson, no quiz questions exist
        await _completionRepo.AddAsync(new StudentLessonCompletion
        {
            StudentId = StudentId,
            LessonId = 1,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        SetupTeacher();
        var gradeDto = new AssignCourseGradeDto { StudentId = StudentId, Mark = 9 };

        var result = await _service.AssignCourseGradeAsync(1, gradeDto, TeacherEmail);

        Assert.True(result.Success);
        var grade = await _gradeRepo.SingleOrDefaultAsync(g => g.CourseId == 1 && g.StudentId == StudentId);
        Assert.NotNull(grade);
        Assert.Equal(9, grade.Mark);
        Assert.Equal(TeacherId, grade.GivenByTeacherId);
    }
}
