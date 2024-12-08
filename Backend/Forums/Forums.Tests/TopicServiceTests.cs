using Forums.DAL.Context;
using Forums.DAL.Models;
using Forums.DL.Repositories;
using Forums.DL.Services;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Forums.API.Infrastructure;
using Moq;
using Forums.DL.Grpc;
using Forums.DAL.SideModels;

namespace Forums.Tests
{
    public class TopicServiceTests
    {

        private IBaseRepository<Topic> TopicRepository { get; set; }
        private IBaseRepository<Like> LikeRepository { get; set; }
        private IBaseRepository<Dislike> DislikeRepository { get; set; }

        private ServiceProvider ServiceProvider { get; set; }

        private TopicService Service { get; set; }

        public TopicServiceTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TopicServiceTest"));

            services.AddTransient<IBaseRepository<Topic>, BaseRepository<Topic>>();
            services.AddTransient<IBaseRepository<Like>, BaseRepository<Like>>();
            services.AddTransient<IBaseRepository<Dislike>, BaseRepository<Dislike>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            TopicRepository = scopedServices.GetRequiredService<IBaseRepository<Topic>>();
            LikeRepository = scopedServices.GetRequiredService<IBaseRepository<Like>>();
            DislikeRepository = scopedServices.GetRequiredService<IBaseRepository<Dislike>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            var autoMapper = config.CreateMapper();

            var mockGrpcClient = new Mock<IUserInfoClient>();

            var reply1 = new UserInfoForTopic()
            {
                IsAllowed = true,
                UserId = 1,
                UniversityId = 1
            };

            var reply2 = new UserInfoForTopic()
            {
                IsAllowed = false,
                UserId = 1,
                UniversityId = 2
            };

            mockGrpcClient
                .Setup(client => client.GetUserInfoForTopic(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((string universityName, string userEmail) =>
                {
                    if (universityName == "DKU")
                        return reply1;
                    else
                        return reply2;
                });

            mockGrpcClient
                .Setup(client => client.GetUniversityName(
                    It.IsAny<long>()))
                .ReturnsAsync((long UniversityId) =>
                {
                    if (UniversityId == 1)
                        return "DKU";
                    else
                        return "None";
                });

            Service = new TopicService(TopicRepository, LikeRepository, DislikeRepository, mockGrpcClient.Object, autoMapper);

            var context = TopicRepository.GetContext();
            context.Database.EnsureDeleted();

            var topic1 = new Topic()
            {
                UniversityId = 1,
                TopicCreatorId = 1,
                TopicTitle = "Test Title",
                TopicContent = "Test Content",
            }
        }

        [Fact]
        public async Task GetTopicsTest()
        {

        }

        [Fact]
        public async Task CreateTopicTest()
        {

        }

        [Fact]
        public async Task LikeTopicTest()
        {

        }

        [Fact]
        public async Task DislikeTopicTest()
        {

        }
    }
}
