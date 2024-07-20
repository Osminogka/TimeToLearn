using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.Context;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;
using Users.DL.Services;

namespace Users.Tests
{
    public class TeacherServiceTest
    {
        private IBaseRepository<University> UniversityRepository { get; set; }
        private IBaseRepository<BaseUser> UserRepository { get; set; }
        private IBaseRepository<Teacher> TeacherRepository { get; set; }
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
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            TeacherRepository = scopedServices.GetRequiredService<IBaseRepository<Teacher>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();

            Service = new TeacherService(TeacherRepository, UserRepository, UniversityRepository, EntryRequestRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            var simpleUser = new BaseUser
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
                IsTeacher = false,
            };
            context.Add(simpleUser);

            BaseUser directorOpen = new BaseUser()
            {
                Id = 2,
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

            var unverifiedTeacher = new BaseUser
            {
                Id = 3,
                OriginalId = Guid.NewGuid(),
                Username = "UnverifiedTeacher",
                Email = "unverifiedteacher@test.com",
                IsTeacher = true
            };

            var unverifiedTeacherStruct = new Teacher()
            {
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
                IsTeacher = true
            };

            var verifiedTeacherStruct = new Teacher()
            {
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
        public async Task InviteTeacherToUniversityTest()
        {
            //Act
            var result = await Service.InviteTeacherToUniversity("DKU", VerifiedTeacher, DirectorEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == VerifiedTeacher && obj.SentByUniversity == true);

            Assert.True(response.Success); ;
            Assert.NotNull(invite);
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
