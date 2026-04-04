using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Core.DAL.Context;
using Core.DAL.Models;
using Core.DL.Repositories;
using Core.DL.Services;
using Microsoft.EntityFrameworkCore;
using Core.DAL.Dtos;
using Core.DAL.SideModels;
using Core.API.Infrastructure;

namespace Users.Tests;

public class UniversityServiceTests
{
    private IBaseRepository<University> UniversityRepository { get; set; }
    private IBaseRepository<BaseUser> UserRepository { get; set; }
    private IBaseRepository<StudentEnrollment> StudentEnrollmentRepository { get; set; }
    private IBaseRepository<TeacherEnrollment> TeacherEnrollmentRepository { get; set; }

    private IUniversityService Service;

    private ServiceProvider ServiceProvider;

    private BaseUser Director = new BaseUser()
    {
        Id = 1,
        OriginalId = Guid.NewGuid(),
        Username = "Osminogka",
        Email = "osminogka@test.com",
        TeacherId = 1
    };

    private BaseUser TeacherUser = new BaseUser()
    {
        Id = 2,
        OriginalId = Guid.NewGuid(),
        Username = "Teacher",
        Email = "teacher@test.com",
        TeacherId = 2
    };

    private BaseUser StudentUser = new BaseUser()
    {
        Id = 3,
        OriginalId = Guid.NewGuid(),
        Username = "Student",
        Email = "student@test.com",
    };

    public UniversityServiceTests()
    {
        var services = new ServiceCollection();

        services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbUniversity"));

        services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
        services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
        services.AddTransient<IBaseRepository<StudentEnrollment>, BaseRepository<StudentEnrollment>>();
        services.AddTransient<IBaseRepository<TeacherEnrollment>, BaseRepository<TeacherEnrollment>>();

        ServiceProvider = services.BuildServiceProvider();

        var scope = ServiceProvider.CreateScope();
        var scopedServices = scope.ServiceProvider;

        UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
        UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
        StudentEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<StudentEnrollment>>();
        TeacherEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<TeacherEnrollment>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        var autoMapper = config.CreateMapper();

        Service = new UniversityService(autoMapper, UniversityRepository, UserRepository, StudentEnrollmentRepository, TeacherEnrollmentRepository);

        var context = UserRepository.GetContext();
        context.Database.EnsureDeleted();

        context.Add(Director);
        context.Add(StudentUser);
        context.Add(TeacherUser);

        University universityDto = new University
        {
            Id = 1,
            Name = "DKU",
            Address = new Address { City = "Almaty", Country = "Kaz", Street = "Pushkina" },
            Description = "Test",
            IsOpened = true,
            DirectorId = Director.Id,
            StudentEnrollments = new List<StudentEnrollment>
            {
                new StudentEnrollment { Id = 1, BaseUserId = StudentUser.Id, UniversityId = 1 }
            },
            TeacherEnrollments = new List<TeacherEnrollment>
            {
                new TeacherEnrollment { Id = 1, BaseUserId = Director.Id, UniversityId = 1 },
                new TeacherEnrollment { Id = 2, BaseUserId = TeacherUser.Id, UniversityId = 1 }
            }
        };

        context.Add(universityDto);

        context.SaveChanges();
    }

    [Fact]
    public async Task CreateUniversityTest()
    {
        CreateUniversityDto universityDto = new CreateUniversityDto
        {
            Name = "Narhoz",
            Address = new Address { City = "Almaty", Country = "Kaz", Street = "Pushkina" },
            Description = "Test",
            IsOpened = true,
        };

        // StudentUser is not yet a director — can create a second university
        var result = await Service.CreateAsync(universityDto, StudentUser.Email);

        var response = Assert.IsType<ResponseWithValue<ReadUniversityDto>>(result);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task CreateUniversity_DirectorCanCreateMultipleTest()
    {
        // Director already directs DKU — should be allowed to create another university now
        CreateUniversityDto universityDto = new CreateUniversityDto
        {
            Name = "Narhoz",
            Address = new Address { City = "Almaty", Country = "Kaz", Street = "Pushkina" },
            Description = "Test",
            IsOpened = true,
        };

        var result = await Service.CreateAsync(universityDto, Director.Email);

        var response = Assert.IsType<ResponseWithValue<ReadUniversityDto>>(result);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetAllTest()
    {
        var result = await Service.GetAllAsync();

        var response = Assert.IsType<ResponseGetEnum<string>>(result);
        Assert.True(response.Success);
        Assert.Single(response.Enum);
    }

    [Fact]
    public async Task GetUniversityTest()
    {
        var result = await Service.GetAsync("DKU");

        var response = Assert.IsType<ResponseWithValue<ReadUniversityDto>>(result);
        Assert.True(response.Success);
        Assert.Equal("DKU", response.Value.Name);
    }

    [Fact]
    public async Task GetUniversityTeachers()
    {
        var result = await Service.GetTeachersAsync("DKU", Director.Email);

        var response = Assert.IsType<ResponseGetEnum<string>>(result);

        Assert.True(response.Success);
        Assert.Equal(2, response.Enum.Count());
        Assert.Contains("Teacher", response.Enum);
    }

    [Fact]
    public async Task GetUniversityStudents()
    {
        var result = await Service.GetStudentsAsync("DKU", Director.Email);

        var response = Assert.IsType<ResponseGetEnum<string>>(result);

        Assert.True(response.Success);
        Assert.Single(response.Enum);
        Assert.Contains("Student", response.Enum);
    }
}
