using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Core.API.Infrastructure;
using Core.DAL.Context;
using Core.DAL.Dtos;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;
using Core.DL.Services;

namespace Users.Tests
{
    public class BaseUserServiceTest
    {
        private IBaseRepository<BaseUser> UserRepository { get; set; }
        private IBaseRepository<EntryRequest> EntryRequestRepository { get; set; }
        private IBaseRepository<University> UniversityRepository { get; set; }
        private IBaseRepository<StudentEnrollment> StudentEnrollmentRepository { get; set; }
        private IBaseRepository<TeacherEnrollment> TeacherEnrollmentRepository { get; set; }

        private BaseUserService Service { get; set; }

        private string UserEmail = "osminogka@test.com";

        public BaseUserServiceTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbBaseUsers"));

            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();
            services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
            services.AddTransient<IBaseRepository<StudentEnrollment>, BaseRepository<StudentEnrollment>>();
            services.AddTransient<IBaseRepository<TeacherEnrollment>, BaseRepository<TeacherEnrollment>>();

            var serviceProvider = services.BuildServiceProvider();

            var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            var autoMapper = config.CreateMapper();

            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();
            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            StudentEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<StudentEnrollment>>();
            TeacherEnrollmentRepository = scopedServices.GetRequiredService<IBaseRepository<TeacherEnrollment>>();

            Service = new BaseUserService(UserRepository, EntryRequestRepository, UniversityRepository, StudentEnrollmentRepository, TeacherEnrollmentRepository, autoMapper);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            // Teacher user receiving an invite
            var user = new BaseUser()
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
                TeacherId = 1
            };

            var user2 = new BaseUser
            {
                Id = 2,
                OriginalId = Guid.NewGuid(),
                Username = "Redter",
                Email = "redter@test.com",
                TeacherId = 2
            };

            var university = new University
            {
                Id = 1,
                Name = "DKU",
                Address = new Address { City = "Almaty", Country = "Kaz", Street = "Pushkina" },
                Description = "Test",
                IsOpened = true,
                DirectorId = 2
            };
            context.Add(university);
            context.Add(user);
            context.Add(user2);

            // Invite from university to user
            var invite = new EntryRequest
            {
                BaseUserId = 1,
                UniversityId = 1,
                SentByUniversity = true,
                InviteAsTeacher = true
            };
            context.Add(invite);

            context.SaveChanges();
        }

        [Fact]
        public async Task GetUsersTest()
        {
            var result = await Service.GetUsersAsync();

            var response = Assert.IsType<ResponseGetEnum<string>>(result);
            Assert.True(response.Success);
            Assert.Equal(2, response.Enum.Count());
        }

        [Fact]
        public async Task GetBaseUserTest()
        {
            var result = await Service.GetBaseUserAsync("Osminogka");

            var response = Assert.IsType<ResponseWithValue<ReadBaseUserDto>>(result);
            Assert.True(response.Success);
            Assert.Equal("Osminogka", response.Value.Username);
        }

        [Fact]
        public async Task UpdateUserInfoTest()
        {
            UpdateUserInfoModel info = new UpdateUserInfoModel
            {
                FirstName = "Peter",
                LastName = "Parker",
                Address = new Address { Country = "USA", City = "New York", Street = "Daun street" },
                Phone = "77777777777"
            };

            UpdateUserInfoModel info2 = new UpdateUserInfoModel
            {
                FirstName = "Peter",
                LastName = "Parker",
                Address = new Address { Country = "Germany", City = null, Street = null },
                Phone = "77777777777"
            };

            var result = await Service.UpdateUserInfoAsync(info, UserEmail);
            var user = await UserRepository.SingleOrDefaultAsync(obj => obj.Email == UserEmail);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.True(response.Success);
            Assert.Equal("USA", user.Address.Country);

            await Service.UpdateUserInfoAsync(info2, UserEmail);
            var user2 = await UserRepository.SingleOrDefaultAsync(obj => obj.Email == UserEmail);

            Assert.Equal("Germany", user2.Address.Country);
            Assert.Null(user2.Address.City);
        }

        [Fact]
        public async Task GetInvitesTest()
        {
            var result = await Service.GetInvitesAsync(UserEmail);

            var response = Assert.IsType<ResponseGetEnum<string>>(result);
            Assert.Equal("DKU", response.Enum.First());
            Assert.Single(response.Enum);
        }

        [Fact]
        public async Task AcceptInviteTest()
        {
            var result = await Service.AcceptInviteAsync("DKU", UserEmail);

            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == UserEmail);
            var enrollment = await TeacherEnrollmentRepository.SingleOrDefaultAsync(
                e => e.BaseUserId == 1 && e.UniversityId == 1);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.True(response.Success);
            Assert.NotNull(enrollment);
            Assert.Null(invite);
        }

        [Fact]
        public async Task RejectInviteTest()
        {
            var result = await Service.RejectInviteAsync("DKU", UserEmail);

            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == UserEmail);
            var enrollment = await TeacherEnrollmentRepository.SingleOrDefaultAsync(
                e => e.BaseUserId == 1 && e.UniversityId == 1);

            var response = Assert.IsType<ResponseMessage>(result);
            Assert.True(response.Success);
            Assert.Null(enrollment);
            Assert.Null(invite);
        }
    }
}
