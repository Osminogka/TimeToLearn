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

public class CourseServiceTests
{
    private readonly IBaseRepository<Course> _courseRepo;
    private readonly IBaseRepository<Lesson> _lessonRepo;
    private readonly IBaseRepository<StudentLessonCompletion> _completionRepo;
    private readonly IBaseRepository<StudentCourseGrade> _gradeRepo;
    private readonly Mock<IUserInfoClient> _grpcMock;
    private readonly CourseService _service;

    private const string TeacherEmail = "teacher@test.com";
    private const string StudentEmail = "student@test.com";
    private const string UniversityName = "TestUniversity";
    private const long UniversityId = 1;
    private const long TeacherId = 10;
    private const long StudentId = 20;

    public CourseServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("TestDbCourseService"));
        services.AddTransient<IBaseRepository<Course>, BaseRepository<Course>>();
        services.AddTransient<IBaseRepository<Lesson>, BaseRepository<Lesson>>();
        services.AddTransient<IBaseRepository<StudentLessonCompletion>, BaseRepository<StudentLessonCompletion>>();
        services.AddTransient<IBaseRepository<StudentCourseGrade>, BaseRepository<StudentCourseGrade>>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        _courseRepo = scoped.GetRequiredService<IBaseRepository<Course>>();
        _lessonRepo = scoped.GetRequiredService<IBaseRepository<Lesson>>();
        _completionRepo = scoped.GetRequiredService<IBaseRepository<StudentLessonCompletion>>();
        _gradeRepo = scoped.GetRequiredService<IBaseRepository<StudentCourseGrade>>();

        var context = scoped.GetRequiredService<DataContext>();
        context.Database.EnsureDeleted();

        context.Add(new Course
        {
            Id = 1,
            Title = "Existing Course",
            Description = "Some description",
            TeacherId = TeacherId,
            UniversityId = UniversityId,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        _grpcMock = new Mock<IUserInfoClient>();
        _service = new CourseService(_courseRepo, _lessonRepo, _completionRepo, _gradeRepo, _grpcMock.Object);
    }

    // Sets up gRPC mock for a teacher accessing the seeded university.
    private void SetupTeacher(string email = TeacherEmail, long userId = TeacherId)
    {
        _grpcMock.Setup(m => m.GetUniversityName(UniversityId)).ReturnsAsync(UniversityName);
        _grpcMock.Setup(m => m.GetUserInfoForCourse(UniversityName, email)).ReturnsAsync(new UserInfoForCourse
        {
            UserId = userId,
            UniversityId = UniversityId,
            IsAllowed = true,
            IsTeacher = true
        });
    }

    // Sets up gRPC mock for a student (member but not teacher/director).
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
    public async Task CreateCourse_Teacher_CreatesSuccessfully()
    {
        SetupTeacher();
        var dto = new CreateCourseDto
        {
            Title = "New Course",
            Description = "A great description",
            UniversityName = UniversityName
        };

        var result = await _service.CreateCourseAsync(dto, TeacherEmail);

        Assert.True(result.Success);
        var saved = await _courseRepo.SingleOrDefaultAsync(c => c.Title == "New Course");
        Assert.NotNull(saved);
        Assert.Equal(TeacherId, saved.TeacherId);
        Assert.Equal(UniversityId, saved.UniversityId);
    }

    [Fact]
    public async Task CreateCourse_EmptyTitle_FailsWithoutCallingGrpc()
    {
        var dto = new CreateCourseDto { Title = "  ", Description = "Desc", UniversityName = UniversityName };

        var result = await _service.CreateCourseAsync(dto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Course title cannot be empty", result.Message);
        // gRPC should not be called for an obviously invalid request
        _grpcMock.Verify(m => m.GetUserInfoForCourse(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateCourse_EmptyDescription_FailsWithoutCallingGrpc()
    {
        var dto = new CreateCourseDto { Title = "Valid Title", Description = "", UniversityName = UniversityName };

        var result = await _service.CreateCourseAsync(dto, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Course description cannot be empty", result.Message);
        _grpcMock.Verify(m => m.GetUserInfoForCourse(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateCourse_Student_Fails()
    {
        SetupStudent();
        var dto = new CreateCourseDto { Title = "Course", Description = "Desc", UniversityName = UniversityName };

        var result = await _service.CreateCourseAsync(dto, StudentEmail);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteCourse_CourseNotFound_Fails()
    {
        var result = await _service.DeleteCourseAsync(999, TeacherEmail);

        Assert.False(result.Success);
        Assert.Equal("Such course doesn't exist", result.Message);
    }

    [Fact]
    public async Task DeleteCourse_Teacher_RemovesCourse()
    {
        SetupTeacher();

        var result = await _service.DeleteCourseAsync(1, TeacherEmail);

        Assert.True(result.Success);
        var deleted = await _courseRepo.SingleOrDefaultAsync(c => c.Id == 1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task UpdateCourse_NullDescriptionInDto_PreservesExistingDescription()
    {
        SetupTeacher();
        var dto = new UpdateCourseDto { CourseId = 1, Title = "Updated Title", Description = null };

        var result = await _service.UpdateCourseAsync(dto, TeacherEmail);

        Assert.True(result.Success);
        var course = await _courseRepo.SingleOrDefaultAsync(c => c.Id == 1);
        Assert.Equal("Updated Title", course!.Title);
        Assert.Equal("Some description", course.Description); // unchanged
    }
}
