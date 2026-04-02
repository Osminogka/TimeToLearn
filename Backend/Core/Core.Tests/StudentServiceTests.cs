using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Core.DAL.Context;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;
using Core.DL.Services;

namespace Users.Tests
{
    public class StudentServiceTests
    {
        private IBaseRepository<University> UniversityRepository { get; set; }
        private IBaseRepository<BaseUser> UserRepository { get; set; }
        private IBaseRepository<Student> StudentsRepository { get; set; }
        private IBaseRepository<Teacher> TeacherRepository { get; set; }
        private IBaseRepository<EntryRequest> EntryRequestRepository { get; set; }

        private ServiceProvider ServiceProvider { get; set; }

        private StudentService Service { get; set; }

        private string UserEmail = "osminogka@test.com";
        private string StudentEmail = "student@test.com";
        private string StudentUsername = "Student";
        private string DirectorOpenEmail = "directorOpen@test.com";
        private string DirectorClosedEmail = "directorClosed@test.com";
        private long UserId = 1;
        private long StudentId = 2;
        private long DirectorOpenId = 3;
        private long DirectorClosedId = 4;
        private long UniversityOpenId = 1;
        private long UninversityCloesId = 2;

        public StudentServiceTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbStudents"));

            services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
            services.AddTransient<IBaseRepository<Student>, BaseRepository<Student>>();
            services.AddTransient<IBaseRepository<Teacher>, BaseRepository<Teacher>>();
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            StudentsRepository = scopedServices.GetRequiredService<IBaseRepository<Student>>();
            TeacherRepository = scopedServices.GetRequiredService<IBaseRepository<Teacher>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();

            Service = new StudentService(StudentsRepository, UserRepository, UniversityRepository, EntryRequestRepository, TeacherRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            var user = new BaseUser()
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
            };
            context.Add(user);

            var studentUser = new BaseUser()
            {
                Id = 2,
                OriginalId = Guid.NewGuid(),
                Username = "Student",
                Email = "student@test.com",
                StudentId = 1
            };
            context.Add(studentUser);

            BaseUser directorOpen = new BaseUser()
            {
                Id = 3,
                OriginalId = Guid.NewGuid(),
                Username = "Director",
                Email = "directorOpen@test.com",
                TeacherId = 1
            };
            context.Add(directorOpen);

            var directorTeacher = new Teacher
            {
                Id = 1,
                BaseUserId = directorOpen.Id,
                IsVerified = true,
                Degree = "Master"
            };
            context.Add(directorTeacher);

            University universityOpen = new University
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
                DirectorId = directorOpen.Id
            };
            context.Add(universityOpen);

            BaseUser directorClosed = new BaseUser()
            {
                Id = 4,
                OriginalId = Guid.NewGuid(),
                Username = "DirectorClosed",
                Email = "directorClosed@test.com",
                TeacherId = 2
            };
            context.Add(directorClosed);

            var directorClosedTeacher = new Teacher
            {
                Id = 2,
                BaseUserId = directorClosed.Id,
                IsVerified = true,
                Degree = "Master"
            };
            context.Add(directorClosedTeacher);

            University universityClosed = new University
            {
                Id = 2,
                Name = "Narhoz",
                Address = new Address
                {
                    City = "Almaty",
                    Country = "Kaz",
                    Street = "Pushkina"
                },
                Description = "Test",
                IsOpened = false,
                DirectorId = directorClosed.Id
            };
            context.Add(universityClosed);

            context.SaveChanges();
        }

        [Fact]
        public async Task BecomeAStudentTest()
        {
            //Act
            var result = await Service.BecomeAStudentAsync(UserEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            var student = await StudentsRepository.SingleOrDefaultAsync(obj => obj.BaseUserId == UserId);

            Assert.True(response.Success);;
            Assert.NotNull(student);
        }

        [Fact]
        public async Task SendRequestToBecomeStudentOfUniversityTest()
        {
            //Act
            var result = await Service.SendRequestToBecomeStudentOfUniversity("DKU", StudentEmail);
            var result2 = await Service.SendRequestToBecomeStudentOfUniversity("DKU", StudentEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            var response2 = Assert.IsType<ResponseMessage>(result2);

            Assert.True(response.Success);
            Assert.False(response2.Success);
        }

        [Fact]
        public async Task SendRequestToClosedUniversityFailsTest()
        {
            //Act
            var result = await Service.SendRequestToBecomeStudentOfUniversity("Narhoz", StudentEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task EntryUniversityTest()
        {
            //Act
            var result = await Service.EntryUniversityAsync("DKU", StudentEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            Assert.True(response.Success);
        }
    }
}
