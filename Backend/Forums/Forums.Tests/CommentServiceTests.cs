using Forums.DAL.Context;
using Forums.DAL.Models;
using Forums.DL.Repositories;
using Forums.DL.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

            var mockGrpcClient = new Mock<IUserInfoClient>();

            // UniversityId=1 → "DKU" → IsAllowed=true, UserId=1
            // UniversityId=2 → "OtherUni" → IsAllowed=false
            // UniversityId=3 → "" → university not found path
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

            Service = new CommentService(TopicRepository, CommentRepository, LikeRepository, DislikeRepository,
                mockGrpcClient.Object);

            var context = TopicRepository.GetContext();
            context.Database.EnsureDeleted();

            // Id=1: UniversityId=1 → accessible via "DKU"
            context.Add(new Topic { Id = 1, UniversityId = 1, CreatorId = 1, TopicTitle = "Topic 1", TopicContent = "Content 1" });
            // Id=2: UniversityId=2 → GetUniversityName returns "OtherUni" → not allowed
            context.Add(new Topic { Id = 2, UniversityId = 2, CreatorId = 2, TopicTitle = "Topic 2", TopicContent = "Content 2" });
            // Id=3: UniversityId=3 → GetUniversityName returns "" → university not found
            context.Add(new Topic { Id = 3, UniversityId = 3, CreatorId = 3, TopicTitle = "Topic 3", TopicContent = "Content 3" });

            // Comments on topic1 (IsTopic=true, PostId=1, UniversityId=1)
            context.Add(new Comment { Id = 1, UniversityId = 1, CreatorId = 1, CommentContent = "Comment 1 on topic1", IsTopic = true, PostId = 1 });
            context.Add(new Comment { Id = 2, UniversityId = 1, CreatorId = 1, CommentContent = "Comment 2 on topic1", IsTopic = true, PostId = 1 });

            // Reply to comment1 (IsTopic=false, PostId=1, UniversityId=1)
            context.Add(new Comment { Id = 3, UniversityId = 1, CreatorId = 1, CommentContent = "Reply to comment1", IsTopic = false, PostId = 1 });

            // Comment on not-allowed topic2 (UniversityId=2)
            context.Add(new Comment { Id = 4, UniversityId = 2, CreatorId = 2, CommentContent = "Comment on topic2", IsTopic = true, PostId = 2 });

            // Comment with university-not-found (UniversityId=3)
            context.Add(new Comment { Id = 5, UniversityId = 3, CreatorId = 3, CommentContent = "Comment on topic3", IsTopic = true, PostId = 3 });

            context.SaveChanges();
        }

        // --- GetCommentsAsync ---

        [Fact]
        public async Task GetComments_ForTopic_ReturnsTopicComments()
        {
            var result = await Service.GetCommentsAsync(true, 1, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.True(response.Success);
            Assert.Equal("You got some comments", response.Message);
            Assert.Equal(2, response.Values.Count);
        }

        [Fact]
        public async Task GetComments_ForComment_ReturnsReplies()
        {
            var result = await Service.GetCommentsAsync(false, 1, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.True(response.Success);
            Assert.Single(response.Values);
        }

        [Fact]
        public async Task GetComments_EnrichesCreatorNameAndRole()
        {
            var result = await Service.GetCommentsAsync(true, 1, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.True(response.Success);
            Assert.All(response.Values, c =>
            {
                Assert.Equal("User1", c.CreatorName);
                Assert.Equal("Student", c.CreatorRole);
            });
        }

        [Fact]
        public async Task GetComments_CountsRepliesForEachComment()
        {
            // comment1 (Id=1) has one reply (comment3, IsTopic=false, PostId=1)
            var result = await Service.GetCommentsAsync(true, 1, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.True(response.Success);
            var comment1Dto = response.Values.First(c => c.Id == 1);
            Assert.Equal(1, comment1Dto.RepliesCount);
        }

        [Fact]
        public async Task GetComments_AfterLike_ShowsCorrectLikeCount()
        {
            await Service.LikeCommentAsync(1, "user@test.com");

            var result = await Service.GetCommentsAsync(true, 1, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.True(response.Success);
            var comment1Dto = response.Values.First(c => c.Id == 1);
            Assert.Equal(1, comment1Dto.LikesOverall);
            Assert.Equal(0, comment1Dto.DislikesOverall);
        }

        [Fact]
        public async Task GetComments_AfterDislike_ShowsCorrectDislikeCount()
        {
            await Service.DislikeCommentAsync(1, "user@test.com");

            var result = await Service.GetCommentsAsync(true, 1, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.True(response.Success);
            var comment1Dto = response.Values.First(c => c.Id == 1);
            Assert.Equal(0, comment1Dto.LikesOverall);
            Assert.Equal(1, comment1Dto.DislikesOverall);
        }

        [Fact]
        public async Task GetComments_NegativePage_Fails()
        {
            var result = await Service.GetCommentsAsync(true, 1, "user@test.com", -1);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.False(response.Success);
            Assert.Equal("Invalid page number", response.Message);
        }

        [Fact]
        public async Task GetComments_TopicNotFound_Fails()
        {
            var result = await Service.GetCommentsAsync(true, 999, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.False(response.Success);
            Assert.Equal("Wrong request", response.Message);
        }

        [Fact]
        public async Task GetComments_CommentRecordNotFound_Fails()
        {
            var result = await Service.GetCommentsAsync(false, 999, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.False(response.Success);
            Assert.Equal("Wrong request", response.Message);
        }

        [Fact]
        public async Task GetComments_UniversityNotFound_Fails()
        {
            // topic3 has UniversityId=3 → GetUniversityName returns ""
            var result = await Service.GetCommentsAsync(true, 3, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.False(response.Success);
            Assert.Equal("University not found", response.Message);
        }

        [Fact]
        public async Task GetComments_NotAllowed_Fails()
        {
            // topic2 has UniversityId=2 → GetUniversityName returns "OtherUni" → IsAllowed=false
            var result = await Service.GetCommentsAsync(true, 2, "user@test.com", 0);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.False(response.Success);
            Assert.Equal("You aren't allowed to get comments for this record", response.Message);
        }

        [Fact]
        public async Task GetComments_SecondPage_ReturnsEmpty()
        {
            // Only 2 comments on topic1; page 1 should return nothing
            var result = await Service.GetCommentsAsync(true, 1, "user@test.com", 1);

            var response = Assert.IsType<ResponseArray<ReadCommentDto>>(result);
            Assert.True(response.Success);
            Assert.Empty(response.Values);
        }

        // --- CreateCommentAsync ---

        [Fact]
        public async Task CreateComment_OnTopic_CreatesCommentAndReturnsSuccess()
        {
            var dto = new CreateCommentDto
            {
                UniversityName = "DKU",
                CommentContent = "New comment on topic",
                IsTopic = true,
                PostId = 1
            };

            var result = await Service.CreateCommentAsync(dto, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You created the comment", result.Message);
            var saved = await CommentRepository.SingleOrDefaultAsync(c => c.CommentContent == "New comment on topic");
            Assert.NotNull(saved);
            Assert.Equal(1, saved.UniversityId);
            Assert.Equal(1, saved.CreatorId);
        }

        [Fact]
        public async Task CreateComment_AsReply_CreatesReplyAndReturnsSuccess()
        {
            var dto = new CreateCommentDto
            {
                UniversityName = "DKU",
                CommentContent = "Reply to comment1",
                IsTopic = false,
                PostId = 1
            };

            var result = await Service.CreateCommentAsync(dto, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You created the comment", result.Message);
            var replies = await CommentRepository.Where(c => !c.IsTopic && c.PostId == 1).CountAsync();
            Assert.Equal(2, replies); // comment3 was seeded + this new one
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

            var result = await Service.CreateCommentAsync(dto, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Comment content cannot be empty", result.Message);
        }

        [Fact]
        public async Task CreateComment_NotAllowed_Fails()
        {
            var dto = new CreateCommentDto
            {
                UniversityName = "OtherUni",
                CommentContent = "Valid content",
                IsTopic = true,
                PostId = 1
            };

            var result = await Service.CreateCommentAsync(dto, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("You don't have such rights", result.Message);
        }

        [Fact]
        public async Task CreateComment_TopicPostNotFound_Fails()
        {
            var dto = new CreateCommentDto
            {
                UniversityName = "DKU",
                CommentContent = "Valid content",
                IsTopic = true,
                PostId = 999
            };

            var result = await Service.CreateCommentAsync(dto, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Such record doesn't exist", result.Message);
        }

        [Fact]
        public async Task CreateComment_CommentPostNotFound_Fails()
        {
            var dto = new CreateCommentDto
            {
                UniversityName = "DKU",
                CommentContent = "Valid content",
                IsTopic = false,
                PostId = 999
            };

            var result = await Service.CreateCommentAsync(dto, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Such record doesn't exist", result.Message);
        }

        [Fact]
        public async Task CreateComment_UniversityIdMismatch_Fails()
        {
            // Reply.UniversityId=1 (from DKU), but topic2 has UniversityId=2
            var dto = new CreateCommentDto
            {
                UniversityName = "DKU",
                CommentContent = "Valid content",
                IsTopic = true,
                PostId = 2
            };

            var result = await Service.CreateCommentAsync(dto, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Such record doesn't exist", result.Message);
        }

        // --- LikeCommentAsync ---

        [Fact]
        public async Task LikeComment_ValidRequest_LikesCommentAndReturnsSuccess()
        {
            var result = await Service.LikeCommentAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You liked the comment", result.Message);
            var likeCount = await LikeRepository.Where(l => !l.IsTopic && l.PostId == 1).CountAsync();
            Assert.Equal(1, likeCount);
        }

        [Fact]
        public async Task LikeComment_CommentNotFound_Fails()
        {
            var result = await Service.LikeCommentAsync(999, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Such comment doesn't exist", result.Message);
        }

        [Fact]
        public async Task LikeComment_UniversityNotFound_Fails()
        {
            // comment5 has UniversityId=3 → GetUniversityName returns ""
            var result = await Service.LikeCommentAsync(5, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("University not found", result.Message);
        }

        [Fact]
        public async Task LikeComment_NotAllowed_Fails()
        {
            // comment4 has UniversityId=2 → GetUniversityName returns "OtherUni" → IsAllowed=false
            var result = await Service.LikeCommentAsync(4, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("You don't have such rights", result.Message);
        }

        [Fact]
        public async Task LikeComment_SecondLike_RemovesLike()
        {
            await Service.LikeCommentAsync(1, "user@test.com");

            var result = await Service.LikeCommentAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You removed your like", result.Message);
            var likeCount = await LikeRepository.Where(l => !l.IsTopic && l.PostId == 1).CountAsync();
            Assert.Equal(0, likeCount);
        }

        [Fact]
        public async Task LikeComment_WhenAlreadyDisliked_RemovesDislikeAndAddsLike()
        {
            await Service.DislikeCommentAsync(1, "user@test.com");

            var result = await Service.LikeCommentAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You liked the comment", result.Message);
            var likeCount = await LikeRepository.Where(l => !l.IsTopic && l.PostId == 1).CountAsync();
            var dislikeCount = await DislikeRepository.Where(d => !d.IsTopic && d.PostId == 1).CountAsync();
            Assert.Equal(1, likeCount);
            Assert.Equal(0, dislikeCount);
        }

        // --- DislikeCommentAsync ---

        [Fact]
        public async Task DislikeComment_ValidRequest_DislikesCommentAndReturnsSuccess()
        {
            var result = await Service.DislikeCommentAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You disliked the comment", result.Message);
            var dislikeCount = await DislikeRepository.Where(d => !d.IsTopic && d.PostId == 1).CountAsync();
            Assert.Equal(1, dislikeCount);
        }

        [Fact]
        public async Task DislikeComment_CommentNotFound_Fails()
        {
            var result = await Service.DislikeCommentAsync(999, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("Such comment doesn't exist", result.Message);
        }

        [Fact]
        public async Task DislikeComment_UniversityNotFound_Fails()
        {
            var result = await Service.DislikeCommentAsync(5, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("University not found", result.Message);
        }

        [Fact]
        public async Task DislikeComment_NotAllowed_Fails()
        {
            var result = await Service.DislikeCommentAsync(4, "user@test.com");

            Assert.False(result.Success);
            Assert.Equal("You don't have such rights", result.Message);
        }

        [Fact]
        public async Task DislikeComment_SecondDislike_RemovesDislike()
        {
            await Service.DislikeCommentAsync(1, "user@test.com");

            var result = await Service.DislikeCommentAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You removed your dislike", result.Message);
            var dislikeCount = await DislikeRepository.Where(d => !d.IsTopic && d.PostId == 1).CountAsync();
            Assert.Equal(0, dislikeCount);
        }

        [Fact]
        public async Task DislikeComment_WhenAlreadyLiked_RemovesLikeAndAddsDislike()
        {
            await Service.LikeCommentAsync(1, "user@test.com");

            var result = await Service.DislikeCommentAsync(1, "user@test.com");

            Assert.True(result.Success);
            Assert.Equal("You disliked the comment", result.Message);
            var likeCount = await LikeRepository.Where(l => !l.IsTopic && l.PostId == 1).CountAsync();
            var dislikeCount = await DislikeRepository.Where(d => !d.IsTopic && d.PostId == 1).CountAsync();
            Assert.Equal(0, likeCount);
            Assert.Equal(1, dislikeCount);
        }
    }
}
