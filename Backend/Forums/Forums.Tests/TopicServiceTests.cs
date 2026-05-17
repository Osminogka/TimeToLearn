using Forums.DAL.Context;
using Forums.DAL.Models;
using Forums.DL.Repositories;
using Forums.DL.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddDbContext<DataContext>(options =>
                options.UseInMemoryDatabase("TopicServiceTest")
                       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            services.AddTransient<IBaseRepository<Topic>, BaseRepository<Topic>>();
            services.AddTransient<IBaseRepository<Like>, BaseRepository<Like>>();
            services.AddTransient<IBaseRepository<Dislike>, BaseRepository<Dislike>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            TopicRepository = scopedServices.GetRequiredService<IBaseRepository<Topic>>();
            LikeRepository = scopedServices.GetRequiredService<IBaseRepository<Like>>();
            DislikeRepository = scopedServices.GetRequiredService<IBaseRepository<Dislike>>();

            var mockGrpcClient = new Mock<IUserInfoClient>();

            // UniversityId=1 → "DKU" → IsAllowed=true, UserId=1
            // UniversityId=2 → "OtherUni" → IsAllowed=false
            // UniversityId=3 → "" (empty) → university not found path
            mockGrpcClient
                .Setup(client => client.GetUniversityName(It.IsAny<long>()))
                .ReturnsAsync((long universityId) =>
                {
                    if (universityId == 1) return "DKU";
                    if (universityId == 3) return string.Empty;
                    return "OtherUni";
                });

            mockGrpcClient
                .Setup(client => client.GetUserInfoForTopic(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string universityName, string userEmail) =>
                {
                    if (universityName == "DKU")
                        return new UserInfoForTopic { IsAllowed = true, UserId = 1, UniversityId = 1 };
                    return new UserInfoForTopic { IsAllowed = false, UserId = 0, UniversityId = 0 };
                });

            mockGrpcClient
                .Setup(client => client.GetUserName(It.IsAny<long>()))
                .ReturnsAsync((long userId) => $"User{userId}");

            mockGrpcClient
                .Setup(client => client.GetUserUniversityRole(It.IsAny<long>(), It.IsAny<long>()))
                .ReturnsAsync((long userId, long universityId) => "Student");

            Service = new TopicService(TopicRepository, LikeRepository, DislikeRepository, mockGrpcClient.Object);

            var context = TopicRepository.GetContext();
            context.Database.EnsureDeleted();

            // Id=1: UniversityId=1 → accessible via "DKU"
            context.Add(new Topic
            {
                Id = 1,
                UniversityId = 1,
                CreatorId = 1,
                TopicTitle = "Test Title",
                TopicContent = "Test Content",
                CreatedAt = DateTime.UtcNow
            });

            // Id=2: UniversityId=2 → GetUniversityName returns "OtherUni" → not allowed
            context.Add(new Topic
            {
                Id = 2,
                UniversityId = 2,
                CreatorId = 2,
                TopicTitle = "Topic 2",
                TopicContent = "Topic 2 Content",
                CreatedAt = DateTime.UtcNow
            });

            // Id=3: UniversityId=3 → GetUniversityName returns "" → university not found
            context.Add(new Topic
            {
                Id = 3,
                UniversityId = 3,
                CreatorId = 3,
                TopicTitle = "Topic 3",
                TopicContent = "Topic 3 Content",
                CreatedAt = DateTime.UtcNow
            });

            context.SaveChanges();
        }

        // --- GetUniversityTopicsAsync ---

        [Fact]
        public async Task GetTopics_ValidRequest_ReturnsOnlyUniversityTopics()
        {
            var result = await Service.GetUniversityTopicsAsync("DKU", "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadTopicDto>>(result);
            Assert.True(response.Success);
            Assert.Equal("You got some topics", response.Message);
            Assert.Single(response.Values);
            Assert.Equal("Test Title", response.Values.First().TopicTitle);
        }

        [Fact]
        public async Task GetTopics_EnrichesCreatorNameAndRole()
        {
            var result = await Service.GetUniversityTopicsAsync("DKU", "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadTopicDto>>(result);
            Assert.True(response.Success);
            var topic = response.Values.First();
            Assert.Equal("User1", topic.CreatorName);
            Assert.Equal("Student", topic.CreatorRole);
        }

        [Fact]
        public async Task GetTopics_NegativePage_Fails()
        {
            var result = await Service.GetUniversityTopicsAsync("DKU", "user@test.com", -1);

            var response = Assert.IsType<ResponseArray<ReadTopicDto>>(result);
            Assert.False(response.Success);
            Assert.Equal("Invalid page number", response.Message);
        }

        [Fact]
        public async Task GetTopics_NotAllowed_Fails()
        {
            var result = await Service.GetUniversityTopicsAsync("OtherUni", "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadTopicDto>>(result);
            Assert.False(response.Success);
            Assert.Equal("You don't have such rights", response.Message);
        }

        [Fact]
        public async Task GetTopics_SecondPage_ReturnsEmpty()
        {
            // Only 1 topic for DKU; page 1 should return nothing
            var result = await Service.GetUniversityTopicsAsync("DKU", "user@test.com", 1);

            var response = Assert.IsType<ResponseArray<ReadTopicDto>>(result);
            Assert.True(response.Success);
            Assert.Empty(response.Values);
        }

        // --- CreateTopicAsync ---

        [Fact]
        public async Task CreateTopic_ValidRequest_CreatesTopicAndReturnsSuccess()
        {
            var dto = new CreateTopicDto
            {
                UniversityName = "DKU",
                TopicTitle = "New Topic",
                TopicContent = "New Content"
            };

            var result = await Service.CreateTopicAsync(dto, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("Topic created", result.Message);
            var saved = await TopicRepository.SingleOrDefaultAsync(t => t.TopicTitle == "New Topic");
            Assert.NotNull(saved);
            Assert.Equal(1, saved.UniversityId);
            Assert.Equal(1, saved.CreatorId);
        }

        [Fact]
        public async Task CreateTopic_EmptyTitle_Fails()
        {
            var dto = new CreateTopicDto
            {
                UniversityName = "DKU",
                TopicTitle = "   ",
                TopicContent = "Some content"
            };

            var result = await Service.CreateTopicAsync(dto, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Topic title cannot be empty", result.Message);
        }

        [Fact]
        public async Task CreateTopic_EmptyContent_Fails()
        {
            var dto = new CreateTopicDto
            {
                UniversityName = "DKU",
                TopicTitle = "Valid title",
                TopicContent = ""
            };

            var result = await Service.CreateTopicAsync(dto, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Topic content cannot be empty", result.Message);
        }

        [Fact]
        public async Task CreateTopic_NotAllowed_Fails()
        {
            var dto = new CreateTopicDto
            {
                UniversityName = "OtherUni",
                TopicTitle = "Title",
                TopicContent = "Content"
            };

            var result = await Service.CreateTopicAsync(dto, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("You don't have such rights", result.Message);
        }

        // --- LikeTopicAsync ---

        [Fact]
        public async Task LikeTopic_ValidRequest_LikesTopicAndIncrementsCounter()
        {
            var result = await Service.LikeTopicAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You liked the topic", result.Message);
            var topic = await TopicRepository.SingleOrDefaultAsync(t => t.Id == 1);
            Assert.Equal(1, topic!.LikesOverall);
        }

        [Fact]
        public async Task LikeTopic_TopicNotFound_Fails()
        {
            var result = await Service.LikeTopicAsync(999, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Such topic doesn't exist", result.Message);
        }

        [Fact]
        public async Task LikeTopic_UniversityNotFound_Fails()
        {
            // topic3 has UniversityId=3 → GetUniversityName returns ""
            var result = await Service.LikeTopicAsync(3, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("University not found", result.Message);
        }

        [Fact]
        public async Task LikeTopic_NotAllowed_Fails()
        {
            // topic2 has UniversityId=2 → GetUniversityName returns "OtherUni" → IsAllowed=false
            var result = await Service.LikeTopicAsync(2, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("You don't have such rights", result.Message);
        }

        [Fact]
        public async Task LikeTopic_SecondLike_RemovesLikeAndDecrementsCounter()
        {
            await Service.LikeTopicAsync(1, "user@test.com");

            var result = await Service.LikeTopicAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You removed your like", result.Message);
            var topic = await TopicRepository.SingleOrDefaultAsync(t => t.Id == 1);
            Assert.Equal(0, topic!.LikesOverall);
        }

        [Fact]
        public async Task LikeTopic_WhenAlreadyDisliked_RemovesDislikeAndAddsLike()
        {
            await Service.DislikeTopicAsync(1, "user@test.com");

            var result = await Service.LikeTopicAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You liked the topic", result.Message);
            var topic = await TopicRepository.SingleOrDefaultAsync(t => t.Id == 1);
            Assert.Equal(1, topic!.LikesOverall);
            Assert.Equal(0, topic.DislikesOverall);
        }

        // --- DislikeTopicAsync ---

        [Fact]
        public async Task DislikeTopic_ValidRequest_DislikesTopicAndIncrementsCounter()
        {
            var result = await Service.DislikeTopicAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You disliked the topic", result.Message);
            var topic = await TopicRepository.SingleOrDefaultAsync(t => t.Id == 1);
            Assert.Equal(1, topic!.DislikesOverall);
        }

        [Fact]
        public async Task DislikeTopic_TopicNotFound_Fails()
        {
            var result = await Service.DislikeTopicAsync(999, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Such topic doesn't exist", result.Message);
        }

        [Fact]
        public async Task DislikeTopic_UniversityNotFound_Fails()
        {
            var result = await Service.DislikeTopicAsync(3, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("University not found", result.Message);
        }

        [Fact]
        public async Task DislikeTopic_NotAllowed_Fails()
        {
            var result = await Service.DislikeTopicAsync(2, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("You don't have such rights", result.Message);
        }

        [Fact]
        public async Task DislikeTopic_SecondDislike_RemovesDislikeAndDecrementsCounter()
        {
            await Service.DislikeTopicAsync(1, "user@test.com");

            var result = await Service.DislikeTopicAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You removed your dislike", result.Message);
            var topic = await TopicRepository.SingleOrDefaultAsync(t => t.Id == 1);
            Assert.Equal(0, topic!.DislikesOverall);
        }

        [Fact]
        public async Task DislikeTopic_WhenAlreadyLiked_RemovesLikeAndAddsDislike()
        {
            await Service.LikeTopicAsync(1, "user@test.com");

            var result = await Service.DislikeTopicAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You disliked the topic", result.Message);
            var topic = await TopicRepository.SingleOrDefaultAsync(t => t.Id == 1);
            Assert.Equal(0, topic!.LikesOverall);
            Assert.Equal(1, topic.DislikesOverall);
        }
    }
}
