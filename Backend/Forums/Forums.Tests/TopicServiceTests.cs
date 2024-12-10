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
using Forums.DAL.Dtos;

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
            };

            var topic2 = new Topic()
            {
                UniversityId = 2,
                TopicCreatorId = 2,
                TopicTitle = "Topic 2",
                TopicContent = "Topic 2 Content"
            };

            context.Add(topic1);
            context.Add(topic2);

            context.SaveChanges();
        }

        [Fact]
        public async Task GetTopicsTest()
        {
            //Arrange
            string userEmail = "tester@gmail.com";
            string universityName = "DKU";

            //Act
            var result = await Service.GetUniversityTopicsAsync(universityName, userEmail, 0);

            //Assert
            var response = Assert.IsType<ResponseArray<ReadTopicDto>>(result);

            Assert.True(response.Success);
            Assert.Single(response.Values);
        }

        [Fact]
        public async Task CreateTopicTest()
        {
            //Arrange
            var createTopicInfo = new CreateTopicDto()
            {
                UniversityName = "DKU",
                TopicTitle = "Topic creation test",
                TopicContent = "I want to test topic creation"
            };
            string creatorEmail = "tester@gmail.com";
            string universityName = "DKU";

            //Act
            var result = await Service.CreateTopicAsync(createTopicInfo, creatorEmail);
            var result2 = await Service.GetUniversityTopicsAsync(universityName, creatorEmail, 0);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            var response2 = Assert.IsType<ResponseArray<ReadTopicDto>>(result2);

            Assert.True(response.Success);

            Assert.True(response2.Success);
            Assert.Equal(2, response2.Values.ToArray().Length);
        }

        [Fact]
        public async Task LikeTopicTest()
        {
            //Arrange
            string userEmail = "tester@gmail.com";
            string universityName = "DKU";
            long topicId = 1;

            //Act
            var result = await Service.LikeTopicAsync(topicId, userEmail);
            var result2 = await Service.GetUniversityTopicsAsync(universityName, userEmail, 0);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            var response2 = Assert.IsType<ResponseArray<ReadTopicDto>>(result2);

            Assert.True(response.Success);

            Assert.True(response2.Success);
            Assert.Equal(1, response2.Values.FirstOrDefault().Likes);
        }

        [Fact]
        public async Task DislikeTopicTest()
        {
            //Arrange
            string userEmail = "tester@gmail.com";
            string universityName = "DKU";
            long topicId = 1;

            //Act
            var result = await Service.DislikeTopicAsync(topicId, userEmail);
            var result2 = await Service.GetUniversityTopicsAsync(universityName, userEmail, 0);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            var response2 = Assert.IsType<ResponseArray<ReadTopicDto>>(result2);

            Assert.True(response.Success);

            Assert.True(response2.Success);
            Assert.Equal(1, response2.Values.FirstOrDefault().Dislikes);
        }
    }
}
