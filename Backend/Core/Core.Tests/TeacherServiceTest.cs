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
        private IBaseRepository<Student> StudentRepository { get; set; }
        private IBaseRepository<EntryRequest> EntryRequestRepository { get; set; }

        private ServiceProvider ServiceProvider { get; set; }

        private TeacherService Service { get; set; }

        private string SimpleUserEmail = "osminogka@test.com";
        private string DirectorEmail = "directorOpen@test.com";
        private string UnverifiedTeacher = "unverifiedteacher@test.com";
        private string VerifiedTeacher = "verifiedteacher@test.com";
        private string VerifiedTeacherUsername = "VerifiedTeacher";

        public TeacherServiceTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbTeachers"));

            services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
            services.AddTransient<IBaseRepository<Teacher>, BaseRepository<Teacher>>();
            services.AddTransient<IBaseRepository<Student>, BaseRepository<Student>>();
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            TeacherRepository = scopedServices.GetRequiredService<IBaseRepository<Teacher>>();
            StudentRepository = scopedServices.GetRequiredService<IBaseRepository<Student>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();

            Service = new TeacherService(TeacherRepository, UserRepository, UniversityRepository, EntryRequestRepository, StudentRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

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
            //Act
            var result = await Service.BecomeTeacherAsync(SimpleUserEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            var teacher = await TeacherRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == SimpleUserEmail);

            Assert.True(response.Success); ;
            Assert.NotNull(teacher);
        }

        [Fact]
        public async Task VerifyStatusTest()
        {
            //Act
            var result = await Service.VerifyStatusAsync(UnverifiedTeacher, "Master");

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            var teacher = await TeacherRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == UnverifiedTeacher && obj.IsVerified == true);

            Assert.True(response.Success); ;
            Assert.NotNull(teacher);
        }


        [Fact]
        public async Task SendRequestToBecomeTeacherOfUniversityTest()
        {
            //Act
            var result = await Service.SendRequestToBecomeTeacherOfUniversity("DKU", VerifiedTeacher);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == VerifiedTeacher && obj.SentByUniversity == false);

            Assert.True(response.Success); ;
            Assert.NotNull(invite);
        }
    }
}
