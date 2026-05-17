using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Core.DAL.Context;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;
using Core.DL.Services;

namespace Users.Tests
{
    public class TeacherServiceTest
    {
        private IBaseRepository<University> UniversityRepository { get; set; }
        private IBaseRepository<BaseUser> UserRepository { get; set; }
        private IBaseRepository<Teacher> TeacherRepository { get; set; }
        private IBaseRepository<StudentEnrollment> StudentEnrollmentRepository { get; set; }
        private IBaseRepository<TeacherEnrollment> TeacherEnrollmentRepository { get; set; }
        private IBaseRepository<EntryRequest> EntryRequestRepository { get; set; }

        private ServiceProvider ServiceProvider { get; set; }

        private TeacherService Service { get; set; }

        private string SimpleUserEmail = "osminogka@test.com";
        private string DirectorEmail = "directorOpen@test.com";
        private string UnverifiedTeacher = "unverifiedteacher@test.com";
        private string VerifiedTeacher = "verifiedteacher@test.com";

        public TeacherServiceTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbTeachers"));

            services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
            services.AddTransient<IBaseRepository<Teacher>, BaseRepository<Teacher>>();
            services.AddTransient<IBaseRepository<StudentEnrollment>, BaseRepository<StudentEnrollment>>();
            services.AddTransient<IBaseRepository<TeacherEnrollment>, BaseRepository<TeacherEnrollment>>();
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            TeacherRepository = scopedServices.GetRequiredService<IBaseRepository<Teacher>>();
            StudentEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<StudentEnrollment>>();
            TeacherEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<TeacherEnrollment>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();

            Service = new TeacherService(TeacherRepository, UserRepository, UniversityRepository, EntryRequestRepository, StudentEnrollmentRepository, TeacherEnrollmentRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            // Implicit student — no TeacherId
            var simpleUser = new BaseUser
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
            };
            context.Add(simpleUser);

            BaseUser directorOpen = new BaseUser()
            {
                Id = 2,
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
                Address = new Address { City = "Almaty", Country = "Kaz", Street = "Pushkina" },
                Description = "Test",
                IsOpened = true,
                DirectorId = directorOpen.Id
            };
            context.Add(universityOpen);

            var unverifiedTeacher = new BaseUser
            {
                Id = 3,
                OriginalId = Guid.NewGuid(),
                Username = "UnverifiedTeacher",
                Email = "unverifiedteacher@test.com",
                TeacherId = 2
            };
            var unverifiedTeacherStruct = new Teacher()
            {
                Id = 2,
                BaseUserId = unverifiedTeacher.Id
            };
            context.Add(unverifiedTeacher);
            context.Add(unverifiedTeacherStruct);

            var verifiedTeacher = new BaseUser()
            {
                Id = 4,
                OriginalId = Guid.NewGuid(),
                Username = "VerifiedTeacher",
                Email = "verifiedteacher@test.com",
                TeacherId = 3
            };
            var verifiedTeacherStruct = new Teacher()
            {
                Id = 3,
                BaseUserId = verifiedTeacher.Id,
                IsVerified = true,
                Degree = "Master"
            };
            context.Add(verifiedTeacher);
            context.Add(verifiedTeacherStruct);

            context.SaveChanges();
        }

        [Fact]
        public async Task BecomeTeacherTest()
        {
            var result = await Service.BecomeTeacherAsync(SimpleUserEmail);

            var response = Assert.IsType<ResponseMessage>(result);

            var teacher = await TeacherRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == SimpleUserEmail);

            Assert.True(response.Success);
            Assert.NotNull(teacher);
        }

        [Fact]
        public async Task BecomeTeacher_WhenAlreadyTeacher_FailsTest()
        {
            var result = await Service.BecomeTeacherAsync(DirectorEmail);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task BecomeTeacher_ClearsStudentEnrollmentsTest()
        {
            var context = UserRepository.GetContext();
            context.Add(new StudentEnrollment { Id = 1, BaseUserId = 1, UniversityId = 1 });
            context.SaveChanges();

            var result = await Service.BecomeTeacherAsync(SimpleUserEmail);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.True(response.Success);

            var enrollment = await StudentEnrollmentRepository.SingleOrDefaultAsync(e => e.BaseUserId == 1);
            Assert.Null(enrollment);
        }

        [Fact]
        public async Task VerifyStatusTest()
        {
            var result = await Service.VerifyStatusAsync(UnverifiedTeacher, "Master");

            var response = Assert.IsType<ResponseMessage>(result);

            var teacher = await TeacherRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == UnverifiedTeacher && obj.IsVerified == true);

            Assert.True(response.Success);
            Assert.NotNull(teacher);
        }

        [Fact]
        public async Task SendRequestToBecomeTeacherOfUniversity_IsDisabled_FailsTest()
        {
            // Business rule: teachers can only join universities by director invite, not by self-request.
            var result = await Service.SendRequestToBecomeTeacherOfUniversity("DKU", VerifiedTeacher);

            var response = Assert.IsType<ResponseMessage>(result);

            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == VerifiedTeacher && obj.SentByUniversity == false);

            Assert.False(response.Success);
            Assert.Null(invite);
        }
    }
}
