using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Core.DAL.Context;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;
using Core.DL.Services;

namespace Users.Tests
{
    public class DirectorServiceTests
    {
        private IBaseRepository<University> UniversityRepository { get; set; }
        private IBaseRepository<BaseUser> UserRepository { get; set; }
        private IBaseRepository<EntryRequest> EntryRequestRepository { get; set; }
        private IBaseRepository<StudentEnrollment> StudentEnrollmentRepository { get; set; }
        private IBaseRepository<TeacherEnrollment> TeacherEnrollmentRepository { get; set; }

        private DirectorService Service { get; set; }

        private readonly string UniversityName = "DKU";
        private readonly string DirectorEmail = "director@test.com";
        private readonly string Username = "Osminogka";
        private readonly string StudentName = "Student";
        private readonly string TeacherName = "Teacher";

        public DirectorServiceTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbDirector"));

            services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
            services.AddTransient<IBaseRepository<Teacher>, BaseRepository<Teacher>>();
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();
            services.AddTransient<IBaseRepository<StudentEnrollment>, BaseRepository<StudentEnrollment>>();
            services.AddTransient<IBaseRepository<TeacherEnrollment>, BaseRepository<TeacherEnrollment>>();

            var serviceProvider = services.BuildServiceProvider();

            var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();
            StudentEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<StudentEnrollment>>();
            TeacherEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<TeacherEnrollment>>();

            Service = new DirectorService(UniversityRepository, EntryRequestRepository, UserRepository, StudentEnrollmentRepository, TeacherEnrollmentRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            // Implicit student (no TeacherId)
            var user = new BaseUser()
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
            };
            context.Add(user);

            // Implicit student
            var student = new BaseUser()
            {
                Id = 2,
                OriginalId = Guid.NewGuid(),
                Username = "Student",
                Email = "student@test.com",
            };
            context.Add(student);

            var teacher = new BaseUser()
            {
                Id = 3,
                OriginalId = Guid.NewGuid(),
                Username = "Teacher",
                Email = "teacher@test.com",
                TeacherId = 1
            };
            var teacherStruct = new Teacher()
            {
                Id = 1,
                BaseUserId = teacher.Id,
                IsVerified = true,
                Degree = "Master"
            };
            context.Add(teacher);
            context.Add(teacherStruct);

            var director = new BaseUser()
            {
                Id = 4,
                OriginalId = Guid.NewGuid(),
                Username = "Director",
                Email = "director@test.com",
                TeacherId = 2
            };
            context.Add(director);

            var directorTeacher = new Teacher()
            {
                Id = 2,
                BaseUserId = director.Id,
                IsVerified = true,
                Degree = "PhD"
            };
            context.Add(directorTeacher);

            var university = new University()
            {
                Id = 1,
                Name = "DKU",
                Address = new Address { City = "Almaty", Country = "Kaz", Street = "Pushkina" },
                Description = "Test",
                IsOpened = true,
                DirectorId = director.Id
            };
            context.Add(university);

            context.SaveChanges();
        }

        [Fact]
        public async Task UpdateUniversityInfoTest()
        {
            UpdateUniversityInfoModel model = new UpdateUniversityInfoModel()
            {
                Name = "DKU",
                Description = "New description",
                Address = new Address { Country = "USA", City = "New York", Street = "Daun street" }
            };

            var result = await Service.UpdateUniversityInfoAsync(model, DirectorEmail);
            var university = await UniversityRepository.SingleOrDefaultAsync(obj => obj.Name == UniversityName);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.True(response.Success);
            Assert.Equal("USA", university!.Address.Country);

            UpdateUniversityInfoModel model2 = new UpdateUniversityInfoModel()
            {
                Name = "DKU",
                Description = "New description",
                Address = new Address { Country = "USA", City = null, Street = null }
            };

            result = await Service.UpdateUniversityInfoAsync(model2, DirectorEmail);
            university = await UniversityRepository.SingleOrDefaultAsync(obj => obj.Name == UniversityName);

            var response2 = Assert.IsType<ResponseMessage>(result);
            Assert.True(response2.Success);
            Assert.Null(university!.Address.City);
        }

        [Fact]
        public async Task InviteStudentToUniversityTest()
        {
            var result = await Service.InviteStudentToUniversityAsync(UniversityName, StudentName, DirectorEmail);

            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj =>
                obj.BaseUser.Username == StudentName && obj.SentByUniversity == true);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.NotNull(invite);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task InviteTeacherToUniversityTest()
        {
            var result = await Service.InviteTeacherToUniversityAsync(UniversityName, TeacherName, DirectorEmail);

            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj =>
                obj.BaseUser.Username == TeacherName && obj.SentByUniversity == true);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.NotNull(invite);
            Assert.True(response.Success);
        }
    }
}
