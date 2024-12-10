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

            Service = new CommentService(TopicRepository, CommentRepository, LikeRepository, DislikeRepository,
                mockGrpcClient.Object);

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
    }
}
