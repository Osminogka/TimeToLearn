using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Core.API.Grpc;
using Core.DAL.Context;
using Core.DAL.Models;
using Core.DL.Repositories;
using UserService;

namespace Users.Tests
{
    public class GrpcServiceTest
    {
        public ServiceProvider ServiceProvider { get; }
        public IBaseRepository<University> UniversityRepository { get; }
        public IBaseRepository<BaseUser> UserRepository { get; }
        public GrpcUserInfoService Service { get; }

        public GrpcServiceTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbGrpc"));

            services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UniversityRepository = scopedServices.GetRequiredService<IBaseRepository<University>>();
            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();

            Service = new GrpcUserInfoService(UserRepository, UniversityRepository);

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

            University universityOpen = new University
            {
                Id = 1,
                Name = "DKU",
                Address = new Address { City = "Almaty", Country = "Kaz", Street = "Pushkina" },
                Description = "Test",
                IsOpened = true,
                DirectorId = directorOpen.Id,
                StudentEnrollments = new List<StudentEnrollment>
                {
                    new StudentEnrollment { Id = 1, BaseUserId = simpleUser.Id, UniversityId = 1 }
                },
                TeacherEnrollments = new List<TeacherEnrollment>
                {
                    new TeacherEnrollment { Id = 1, BaseUserId = directorOpen.Id, UniversityId = 1 }
                }
            };
            context.Add(universityOpen);

            context.SaveChanges();
        }

        [Fact]
        public async Task GetInfoForTopic()
        {
            GetInfoRequest getInfo = new GetInfoRequest()
            {
                UniversityName = "DKU",
                Useremail = "osminogka@test.com"
            };

            var mockServerCallContext = new Mock<ServerCallContext>(MockBehavior.Strict);
            var reply = await Service.GetInfoForTopic(getInfo, mockServerCallContext.Object);

            Assert.Equal(1, reply.UniversityId);
            Assert.Equal(1, reply.UserId);
            Assert.True(reply.IsAllowed);
        }

        [Fact]
        public async Task GetInfoForTopic_Director_IsAllowed()
        {
            GetInfoRequest getInfo = new GetInfoRequest()
            {
                UniversityName = "DKU",
                Useremail = "directorOpen@test.com"
            };

            var mockServerCallContext = new Mock<ServerCallContext>(MockBehavior.Strict);
            var reply = await Service.GetInfoForTopic(getInfo, mockServerCallContext.Object);

            Assert.Equal(1, reply.UniversityId);
            Assert.Equal(2, reply.UserId);
            Assert.True(reply.IsAllowed);
            Assert.True(reply.IsDirector);
        }

        [Fact]
        public async Task GetUniversityName()
        {
            UniversityId id = new UniversityId() { UniversityId_ = 1 };

            var mockServerCallContext = new Mock<ServerCallContext>(MockBehavior.Strict);
            var reply = await Service.GetUniversityName(id, mockServerCallContext.Object);

            Assert.Equal("DKU", reply.UniversityName_);
        }

        [Fact]
        public async Task GetUserName()
        {
            UserId id = new UserId() { UserId_ = 1 };

            var mockServerCallContext = new Mock<ServerCallContext>(MockBehavior.Strict);
            var reply = await Service.GetUserName(id, mockServerCallContext.Object);

            Assert.Equal("Osminogka", reply.Username);
        }

        [Fact]
        public async Task GetUserUniversityRole_Student()
        {
            UserUniversityRequest request = new UserUniversityRequest
            {
                UserId = 1,
                UniversityId = 1
            };

            var mockServerCallContext = new Mock<ServerCallContext>(MockBehavior.Strict);
            var reply = await Service.GetUserUniversityRole(request, mockServerCallContext.Object);

            Assert.Equal("Student", reply.Role);
        }

        [Fact]
        public async Task GetUserUniversityRole_Manager()
        {
            UserUniversityRequest request = new UserUniversityRequest
            {
                UserId = 2,
                UniversityId = 1
            };

            var mockServerCallContext = new Mock<ServerCallContext>(MockBehavior.Strict);
            var reply = await Service.GetUserUniversityRole(request, mockServerCallContext.Object);

            Assert.Equal("Manager", reply.Role);
        }
    }
}
