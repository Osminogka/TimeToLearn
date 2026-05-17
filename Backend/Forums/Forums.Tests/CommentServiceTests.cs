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
    public class CommentServiceTests
    {
        private IBaseRepository<Comment> CommentRepository { get; set; }
        private IBaseRepository<Topic> TopicRepository { get; set; }
        private IBaseRepository<Like> LikeRepository { get; set; }
        private IBaseRepository<Dislike> DislikeRepository { get; set; }

        private ServiceProvider ServiceProvider { get; set; }

        private CommentService Service { get; set; }

        public CommentServiceTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("CommentServiceTest"));

            services.AddTransient<IBaseRepository<Comment>, BaseRepository<Comment>>();
            services.AddTransient<IBaseRepository<Topic>, BaseRepository<Topic>>();
            services.AddTransient<IBaseRepository<Like>, BaseRepository<Like>>();
            services.AddTransient<IBaseRepository<Dislike>, BaseRepository<Dislike>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            CommentRepository = scopedServices.GetRequiredService<IBaseRepository<Comment>>();
            TopicRepository = scopedServices.GetRequiredService<IBaseRepository<Topic>>();
            LikeRepository = scopedServices.GetRequiredService<IBaseRepository<Like>>();
            DislikeRepository = scopedServices.GetRequiredService<IBaseRepository<Dislike>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

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

            var autoMapper = config.CreateMapper();

            var mockGrpcClient = new Mock<IUserInfoClient>();

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
                .Setup(client => client.GetUserName(
                    It.IsAny<long>()))
                .ReturnsAsync((long userId) =>
                {
                    if (userId == 1)
                        return "tester";
                    else
                        return "None";
                });

            Service = new CommentService(TopicRepository, CommentRepository, LikeRepository, DislikeRepository,
                mockGrpcClient.Object);

            var context = TopicRepository.GetContext();
            context.Database.EnsureDeleted();

            var topic1 = new Topic()
            {
                Id = 1,
                UniversityId = 1,
                CreatorId = 1,
                TopicTitle = "Test Title",
                TopicContent = "Test Content",
            };

            var topic2 = new Topic()
            {
                Id = 2,
                UniversityId = 2,
                CreatorId = 2,
                TopicTitle = "Topic 2",
                TopicContent = "Topic 2 Content"
            };

            var comment1 = new Comment()
            {
                Id = 1,
                UniversityId = 1,
                CreatorId = 1,
                CommentContent = "Comment for topic1",
                IsTopic = true,
                PostId = 1
            };

            var comment2 = new Comment()
            {
                Id = 2,
                UniversityId = 1,
                CreatorId = 1,
                CommentContent = "Comment number 2 for topic1",
                IsTopic = true,
                PostId = 1
            };

            var comment3 = new Comment()
            {
                Id = 3,
                UniversityId = 1,
                CreatorId = 1,
                CommentContent = "Comment for comment",
                IsTopic = false,
                PostId = 1
            };

            context.Add(topic1);
            context.Add(topic2);
            context.Add(comment1);
            context.Add(comment2);
            context.Add(comment3);

            context.SaveChanges();
        }

        [Fact]
        public async Task GetCommentsTest()
        {
            //Arrange
            string userEmail = "tester@gmail.com";

            //Act
            var result = await Service.GetCommentsAsync(true, 1, userEmail, 0);

            //Assert
            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);

            Assert.True(response.Success);
            Assert.Equal(2, response.Values.ToArray().Length);
        }

        [Fact]
        public async Task CreateCommentsTest()
        {
            //Arrange
            CreateCommentDto comment = new CreateCommentDto()
            {
                UniversityName = "DKU",
                CommentContent = "Test Creation of comment",
                IsTopic = false,
                PostId = 1
            };
            string userEmail = "tester@gmail.com";

            //Act
            var result1 = await Service.CreateCommentAsync(comment, userEmail);
            var result2 = await Service.GetCommentsAsync(false, 1, userEmail, 0);

            //Assert
            var response1 = Assert.IsType<ResponseMessage>(result1);
            var response2 = Assert.IsType<ResponseArray<ReadCommentDto>>(result2);

            Assert.True(response1.Success);

            Assert.True(response2.Success);
            Assert.Equal(2, response2.Values.ToArray().Length);
        }

        [Fact]
        public async Task LikeCommentsTest()
        {
            //Arrange
            string userEmail = "tester@gmail.com";

            //Act
            var result1 = await Service.LikeCommentAsync(1, userEmail);
            var result2 = await Service.GetCommentsAsync(true, 1, userEmail, 0);

            //Assert
            var response1 = Assert.IsType<ResponseMessage>(result1);
            var response2 = Assert.IsType<ResponseArray<ReadCommentDto>>(result2);

            Assert.True(result1.Success);

            Assert.True(result2.Success);
            Assert.Equal(1, response2.Values.FirstOrDefault().LikesOverall);
        }

        [Fact]
        public async Task DislikeCommentsTest()
        {
            //Arrange
            string userEmail = "tester@gmail.com";

            //Act
            var result1 = await Service.DislikeCommentAsync(1, userEmail);
            var result2 = await Service.GetCommentsAsync(true, 1, userEmail, 0);

            //Assert
            var response1 = Assert.IsType<ResponseMessage>(result1);
            var response2 = Assert.IsType<ResponseArray<ReadCommentDto>>(result2);

            Assert.True(result1.Success);

            Assert.True(result2.Success);
            Assert.Equal(1, response2.Values.FirstOrDefault().DislikesOverall);
        }

        // --- additional edge-case tests ---

        [Fact]
        public async Task GetComments_NegativePage_Fails()
        {
            var result = await Service.GetCommentsAsync(true, 1, "tester@gmail.com", -1);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.False(response.Success);
            Assert.Equal("Invalid page number", response.Message);
        }

        [Fact]
        public async Task GetComments_TopicNotFound_Fails()
        {
            var result = await Service.GetCommentsAsync(true, 999, "tester@gmail.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task CreateComment_EmptyContent_Fails()
        {
            var dto = new CreateCommentDto
            {
                UniversityName = "DKU",
                CommentContent = "   ",
                IsTopic = true,
                PostId = 1
            };

            var result = await Service.CreateCommentAsync(dto, "tester@gmail.com");

            Assert.False(result.Success);
            Assert.Equal("Comment content cannot be empty", result.Message);
        }

        [Fact]
        public async Task CreateComment_PostNotFound_Fails()
        {
            var dto = new CreateCommentDto
            {
                UniversityName = "DKU",
                CommentContent = "Valid content",
                IsTopic = true,
                PostId = 999
            };

            var result = await Service.CreateCommentAsync(dto, "tester@gmail.com");

            Assert.False(result.Success);
            Assert.Equal("Such record doesn't exist", result.Message);
        }

        [Fact]
        public async Task LikeComment_CommentNotFound_Fails()
        {
            var result = await Service.LikeCommentAsync(999, "tester@gmail.com");

            Assert.False(result.Success);
            Assert.Equal("Such comment doesn't exist", result.Message);
        }

        [Fact]
        public async Task LikeComment_SecondLike_RemovesLike()
        {
            string userEmail = "tester@gmail.com";

            var first = await Service.LikeCommentAsync(1, userEmail);
            Assert.True(first.Success);

            var second = await Service.LikeCommentAsync(1, userEmail);

            Assert.True(second.Success);
            Assert.Equal("You removed your like", second.Message);

            var likeCount = await LikeRepository.Where(l => l.IsTopic == false && l.PostId == 1).CountAsync();
            Assert.Equal(0, likeCount);
        }

        [Fact]
        public async Task DislikeComment_SecondDislike_RemovesDislike()
        {
            string userEmail = "tester@gmail.com";

            var first = await Service.DislikeCommentAsync(1, userEmail);
            Assert.True(first.Success);

            var second = await Service.DislikeCommentAsync(1, userEmail);

            Assert.True(second.Success);
            Assert.Equal("You removed your dislike", second.Message);

            var dislikeCount = await DislikeRepository.Where(d => d.IsTopic == false && d.PostId == 1).CountAsync();
            Assert.Equal(0, dislikeCount);
        }

        [Fact]
        public async Task DislikeComment_WhenLiked_SwitchesToDislike()
        {
            string userEmail = "tester@gmail.com";

            await Service.LikeCommentAsync(1, userEmail);

            var result = await Service.DislikeCommentAsync(1, userEmail);

            Assert.True(result.Success);
            Assert.Equal("You disliked the comment", result.Message);

            var likeCount = await LikeRepository.Where(l => l.IsTopic == false && l.PostId == 1).CountAsync();
            var dislikeCount = await DislikeRepository.Where(d => d.IsTopic == false && d.PostId == 1).CountAsync();
            Assert.Equal(0, likeCount);
            Assert.Equal(1, dislikeCount);
        }
    }
}
