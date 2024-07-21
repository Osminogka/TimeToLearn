using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Users.DAL.Context;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;
using Users.DL.Services;

namespace Users.Tests
{
    public class StudentServiceTests
    {
        private IBaseRepository<University> UniversityRepository { get; set; }
        private IBaseRepository<BaseUser> UserRepository { get; set; }
        private IBaseRepository<Student> StudentsRepository { get; set; }
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
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            StudentsRepository = scopedServices.GetRequiredService<IBaseRepository<Student>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();

            Service = new StudentService(StudentsRepository, UserRepository, UniversityRepository, EntryRequestRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            var user = new BaseUser()
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
                IsTeacher = false,
            };
            context.Add(user);

            var student = new BaseUser()
            {
                Id = 2,
                OriginalId = Guid.NewGuid(),
                Username = "Student",
                Email = "student@test.com",
                IsTeacher = false,
                StudentId = 1
            };
            context.Add(student);

            BaseUser directorOpen = new BaseUser()
            {
                Id = 3,
                OriginalId = Guid.NewGuid(),
                Username = "Director",
                Email = "directorOpen@test.com",
                IsTeacher = true
            };
            context.Add(directorOpen);

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
                Username = "Director",
                Email = "directorClosed@test.com",
                IsTeacher = true
            };
            context.Add(directorClosed);

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
        public async Task InviteStudentToUniversityTest()
        {
            //Act
            var result = await Service.InviteStudentToUniversity("DKU", StudentUsername, DirectorOpenEmail);
            var result2 = await Service.InviteStudentToUniversity("DKU", StudentUsername, DirectorOpenEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            var response2 = Assert.IsType<ResponseMessage>(result2);

            var entryRequest = await EntryRequestRepository.SingleOrDefaultAsync(obj => obj.UniversityId == 1 && 
                obj.BaseUserId == StudentId && obj.SentByUniversity == true);

            Assert.True(response.Success);
            Assert.False(response2.Success);
            Assert.NotNull(response);
        }

        [Fact]
        public async Task SendRequestToBecomeStudentOfUniversityTest()
        {
            //Act
            var result = await Service.SendRequestToBecomeStudentOfUniversity("Narhoz", StudentEmail);
            var result2 = await Service.SendRequestToBecomeStudentOfUniversity("Narhoz", StudentEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            var response2 = Assert.IsType<ResponseMessage>(result2);

            var entryRequest = await EntryRequestRepository.SingleOrDefaultAsync(obj => obj.UniversityId == 1 &&
                obj.BaseUserId == StudentId && obj.SentByUniversity == false);

            Assert.True(response.Success);
            Assert.False(response2.Success);
            Assert.NotNull(response);
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
