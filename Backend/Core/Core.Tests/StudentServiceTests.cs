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
        private IBaseRepository<StudentEnrollment> StudentEnrollmentRepository { get; set; }
        private IBaseRepository<TeacherEnrollment> TeacherEnrollmentRepository { get; set; }
        private IBaseRepository<Teacher> TeacherRepository { get; set; }
        private IBaseRepository<EntryRequest> EntryRequestRepository { get; set; }

        private ServiceProvider ServiceProvider { get; set; }

        private StudentService Service { get; set; }

        private string UserEmail = "osminogka@test.com";
        private string StudentEmail = "student@test.com";
        private string TeacherEmail = "directorOpen@test.com";
        private long UserId = 1;
        private long StudentId = 2;
        private long TeacherId = 3;
        private long UniversityOpenId = 1;

        public StudentServiceTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbStudents"));

            services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
            services.AddTransient<IBaseRepository<StudentEnrollment>, BaseRepository<StudentEnrollment>>();
            services.AddTransient<IBaseRepository<TeacherEnrollment>, BaseRepository<TeacherEnrollment>>();
            services.AddTransient<IBaseRepository<Teacher>, BaseRepository<Teacher>>();
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            StudentEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<StudentEnrollment>>();
            TeacherEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<TeacherEnrollment>>();
            TeacherRepository = scopedServices.GetRequiredService<IBaseRepository<Teacher>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();

            Service = new StudentService(StudentEnrollmentRepository, TeacherEnrollmentRepository, UserRepository, UniversityRepository, EntryRequestRepository, TeacherRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            // Simple user with no role (implicit student)
            var user = new BaseUser()
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
            };
            context.Add(user);

            // Another implicit student
            var studentUser = new BaseUser()
            {
                Id = 2,
                OriginalId = Guid.NewGuid(),
                Username = "Student",
                Email = "student@test.com",
            };
            context.Add(studentUser);

            // Teacher user (to test switching back to student)
            var teacherUser = new BaseUser()
            {
                Id = 3,
                OriginalId = Guid.NewGuid(),
                Username = "Director",
                Email = "directorOpen@test.com",
                TeacherId = 1
            };
            context.Add(teacherUser);

            var teacherStruct = new Teacher
            {
                Id = 1,
                BaseUserId = teacherUser.Id,
                IsVerified = true,
                Degree = "Master"
            };
            context.Add(teacherStruct);

            var directorClosed = new BaseUser()
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

            University universityOpen = new University
            {
                Id = 1,
                Name = "DKU",
                Address = new Address { City = "Almaty", Country = "Kaz", Street = "Pushkina" },
                Description = "Test",
                IsOpened = true,
                DirectorId = teacherUser.Id
            };
            context.Add(universityOpen);

            University universityClosed = new University
            {
                Id = 2,
                Name = "Narhoz",
                Address = new Address { City = "Almaty", Country = "Kaz", Street = "Pushkina" },
                Description = "Test",
                IsOpened = false,
                DirectorId = directorClosed.Id
            };
            context.Add(universityClosed);

            context.SaveChanges();
        }

        [Fact]
        public async Task BecomeAStudent_WhenTeacher_SwitchesRoleTest()
        {
            // Act: teacher switches back to student
            var result = await Service.BecomeAStudentAsync(TeacherEmail);

            // Assert
            var response = Assert.IsType<ResponseMessage>(result);
            Assert.True(response.Success);

            var user = await UserRepository.SingleOrDefaultAsync(obj => obj.Email == TeacherEmail);
            Assert.Null(user!.TeacherId);

            var teacher = await TeacherRepository.SingleOrDefaultAsync(obj => obj.BaseUserId == TeacherId);
            Assert.Null(teacher);
        }

        [Fact]
        public async Task BecomeAStudent_WhenAlreadyStudent_FailsTest()
        {
            // Act: already a student (no TeacherId), should fail
            var result = await Service.BecomeAStudentAsync(StudentEmail);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task SendRequestToBecomeStudentOfUniversityTest()
        {
            var result = await Service.SendRequestToBecomeStudentOfUniversity("DKU", StudentEmail);
            var result2 = await Service.SendRequestToBecomeStudentOfUniversity("DKU", StudentEmail);

            var response = Assert.IsType<ResponseMessage>(result);
            var response2 = Assert.IsType<ResponseMessage>(result2);

            Assert.True(response.Success);
            Assert.False(response2.Success);
        }

        [Fact]
        public async Task SendRequestToClosedUniversityFailsTest()
        {
            var result = await Service.SendRequestToBecomeStudentOfUniversity("Narhoz", StudentEmail);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task EntryUniversityTest()
        {
            var result = await Service.EntryUniversityAsync("DKU", StudentEmail);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.True(response.Success);

            var enrollment = await StudentEnrollmentRepository.SingleOrDefaultAsync(
                e => e.BaseUserId == StudentId && e.UniversityId == UniversityOpenId);
            Assert.NotNull(enrollment);
        }
    }
}
