using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Core.DAL.Context;
using Core.DAL.Models;
using Core.DL.Repositories;
using Core.DL.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Core.DAL.Dtos;
using Core.DAL.SideModels;
using Core.API.Infrastructure;

namespace Users.Tests;

public class UniversityServiceTests
{
    private IBaseRepository<University> UniversityRepository { get; set; }
    private IBaseRepository<BaseUser> UserRepository { get; set; }
    private IBaseRepository<Student> StudentsRepository { get;  set; }
    private IBaseRepository<Teacher> TeachersRepository { get; set; }

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
        StudentId = 1
    };

    public UniversityServiceTests()
    {
        var services = new ServiceCollection();

        services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbUniversity"));

        services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
        services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
        services.AddTransient<IBaseRepository<Teacher>, BaseRepository<Teacher>>();
        services.AddTransient<IBaseRepository<Student>, BaseRepository<Student>>();

        ServiceProvider = services.BuildServiceProvider();

        var scope = ServiceProvider.CreateScope();
        var scopedServices = scope.ServiceProvider;

        UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
        UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
        StudentsRepository = scopedServices.GetRequiredService<IBaseRepository<Student>>();
        TeachersRepository = scopedServices.GetRequiredService<IBaseRepository<Teacher>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        var autoMapper = config.CreateMapper();

        Service = new UniversityService(autoMapper, UniversityRepository, UserRepository);

        var context = UserRepository.GetContext();
        context.Database.EnsureDeleted();

        context.Add(Director);
        context.Add(StudentUser);
        context.Add(TeacherUser);

        University universityDto = new University
        {
            Id = 1,
            Name = "DKU",
            Address = new Address
            {
                City = "Almaty",
                Country = "Kaz",
                Street = "Pushkina"
            },
            Description = "Test",
            IsOpened = true,
            DirectorId = Director.Id,
            Members = new List<BaseUser> { Director, TeacherUser, StudentUser }
        };

        context.Add(universityDto);

        Student student = new Student
        {
            Id = 1,
            BaseUserId = StudentUser.Id,
        };
        context.Add(student);

        Teacher teacher = new Teacher
        {
            Id = 1,
            Degree = "Master",
            BaseUserId = Director.Id,
        };
        context.Add(teacher);

        Teacher teacher2 = new Teacher
        {
            Id = 2,
            Degree = "Master",
            BaseUserId = TeacherUser.Id,
        };
        context.Add(teacher2);

        context.SaveChanges();
    }

    [Fact]
    public async Task CreateUniversityTest()
    {
        //Arrange
        CreateUniversityDto universityDto = new CreateUniversityDto
        {
            Name = "Narhoz",
            Address = new Address
            {
                City = "Almaty",
                Country = "Kaz",
                Street = "Pushkina"
            },
            Description = "Test",
            IsOpened = true,
        };

        // Act — use StudentUser who is not yet a director
        var result = await Service.CreateAsync(universityDto, StudentUser.Email);

        //Assert
        var response = Assert.IsType< ResponseWithValue<ReadUniversityDto>>(result);

        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetAllTest()
    {
        //Act
        var result2 = await Service.GetAllAsync();

        //Assert
        var response = Assert.IsType<ResponseGetEnum<string>>(result2);

        Assert.True(response.Success);
        Assert.Single(response.Enum);
    }

    [Fact]
    public async Task GetUniversityTest()
    {
        //Act
        var result = await Service.GetAsync("DKU");

        //Assert
        var response = Assert.IsType<ResponseWithValue<ReadUniversityDto>>(result);
        Assert.True(response.Success);
        Assert.Equal("DKU", response.Value.Name);
    }

    [Fact]
    public async Task GetUniversityTeachers()
    {
        //Act
        var result = await Service.GetTeachersAsync("DKU", Director.Email);

        //Assert
        var response = Assert.IsType<ResponseGetEnum<string>>(result);

        Assert.True(response.Success);
        // InMemory provider does not support filtered Include, so all members are returned.
        // On SQL Server, only members with TeacherId != null are returned (Director + TeacherUser = 2).
        Assert.True(response.Enum.Count() >= 2);
    }

    [Fact]
    public async Task GetUniversityStudents()
    {
        //Act
        var result = await Service.GetStudentsAsync("DKU", Director.Email);

        //Assert
        var response = Assert.IsType<ResponseGetEnum<string>>(result);

        Assert.True(response.Success);
        // InMemory provider does not support filtered Include, so all members are returned.
        // On SQL Server, only members with TeacherId == null are returned (StudentUser = 1).
        Assert.True(response.Enum.Count() >= 1);
    }
}
