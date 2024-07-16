using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Users.DAL.Context;
using Users.DAL.Models;
using Users.DL.Repositories;
using Users.DL.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Users.DAL.Dtos;
using Users.DAL.SideModels;
using Users.API.Infrastructure;

namespace Users.Tests;

public class UniversityServiceTests
{
    private IBaseRepository<University> UniversityRepository { get; set; }
    private IBaseRepository<BaseUser> UserRepository { get; set; }
    private IBaseRepository<Student> StudentsRepository { get;  set; }
    private IBaseRepository<Teacher> TeachersRepository { get; set; }

    private IUniversityService Service;

    private ServiceProvider ServiceProvider;

    private BaseUser User = new BaseUser()
    {
        Id = 1,
        OriginalId = Guid.NewGuid(),
        Username = "Osminogka",
        Email = "osminogka@test.com",
        UniversityId = 1,
        IsTeacher = true
    };

    private BaseUser Teacher = new BaseUser()
    {
        Id = 2,
        OriginalId = Guid.NewGuid(),
        Username = "Teacher",
        Email = "teacher@test.com",
        UniversityId = 1,
        IsTeacher = true
    };

    private BaseUser Student = new BaseUser()
    {
        Id = 3,
        OriginalId = Guid.NewGuid(),
        Username = "Student",
        Email = "student@test.com",
        UniversityId = 1,
        IsTeacher = false
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

        context.Add(User);
        context.Add(Student);
        context.Add(Teacher);

        //Create university
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
            DirectorId = User.Id
        };

        
        context.Add(universityDto);


        Student student = new Student
        {
            BaseUserId = User.Id,
        };

        context.Add(student);

        Teacher teacher = new Teacher
        {
            Degree = "Master",
            BaseUserId = Teacher.Id,
        };
        
        context.Add(teacher);

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

        // Act
        var result = await Service.CreateAsync(universityDto, User.Email);

        //Assert
        var response = Assert.IsType<ResponseUniversity>(result);

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
        var response = Assert.IsType<ResponseUniversity>(result);
        Assert.True(response.Success);
        Assert.Equal("DKU", response.UniversityDto.Name);
    }

    [Fact]
    public async Task GetUniversityTeachers()
    {
        //Act
        var result = await Service.GetTeachersAsync("DKU", User.Email);
        
        //Assert
        var response = Assert.IsType<ResponseGetEnum<string>>(result);
        
        Assert.True(response.Success);
        Assert.Equal(3, response.Enum.Count());
    }

    [Fact]
    public async Task GetUniversityStudents()
    {
        //Act
        var result = await Service.GetStudentsAsync("DKU", User.Email);

        //Assert
        var response = Assert.IsType<ResponseGetEnum<string>>(result);

        Assert.True(response.Success);
        Assert.Equal(3, response.Enum.Count());
    }
}