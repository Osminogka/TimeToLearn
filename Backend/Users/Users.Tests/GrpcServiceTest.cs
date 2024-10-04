using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Users.API.Grpc;
using Users.DAL.Context;
using Users.DAL.Models;
using Users.DL.Repositories;
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
                UniversityId = 1,
                IsTeacher = false,
            };
            context.Add(simpleUser);

            BaseUser directorOpen = new BaseUser()
            {
                Id = 2,
                OriginalId = Guid.NewGuid(),
                Username = "Director",
                Email = "directorOpen@test.com",
                UniversityId = 1,
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

            context.SaveChanges();
        }

        [Fact]
        public async Task GetInfoForTopic()
        {
            //Arrange
            GetInfoRequest getInfo = new GetInfoRequest()
            {
                UniversityName = "DKU",
                Useremail = "osminogka@test.com"
            };

            var mockServerCallContext = new Mock<ServerCallContext>(MockBehavior.Strict);
            //Act
            var reply = await Service.GetInfoForTopic(getInfo, mockServerCallContext.Object);

            //Assert
            Assert.Equal(1, reply.UniversityId);
            Assert.Equal(1, reply.UserId);
            Assert.True(reply.IsAllowed);
        }

        [Fact]
        public async Task GetUniversityName()
        {
            //Arrange
            UserId id = new UserId()
            {
                UserId_ = 1
            };

            var mockServerCallContext = new Mock<ServerCallContext>(MockBehavior.Strict);
            //Act
            var reply = await Service.GetUserName(id, mockServerCallContext.Object);

            //Assert
            Assert.Equal("Osminogka", reply.Username);
        }

        [Fact]
        public async Task GetInfoFoGetUserNamerTopic()
        {
            //Arrange
            UniversityId id = new UniversityId()
            {
                UniversityId_ = 1
            };

            var mockServerCallContext = new Mock<ServerCallContext>(MockBehavior.Strict);
            //Act
            var reply = await Service.GetUniversityName(id, mockServerCallContext.Object);

            //Assert
            Assert.Equal("DKU", reply.UniversityName_);
        }
    }
}
