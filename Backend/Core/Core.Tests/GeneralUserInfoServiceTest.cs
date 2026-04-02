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
    public class GeneralUserInfoServiceTest
    {
        private IBaseRepository<BaseUser> UserRepository { get; set; }

        private GeneralUserInfoService Service { get; set; }

        private string UserEmail = "osminogka@test.com";

        public GeneralUserInfoServiceTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbGeneralInfo"));

            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

            var serviceProvider = services.BuildServiceProvider();

            var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();

            Service = new GeneralUserInfoService(UserRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

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
                StudentId = 1
            };

            var user3 = new BaseUser
            {
                Id = 3,
                OriginalId = Guid.NewGuid(),
                Username = "Nobody",
                Email = "none@test.com"
            };

            context.Add(user);
            context.Add(user2);
            context.Add(user3);

            context.SaveChanges();
        }

        [Fact]
        public async Task GetRoleTest()
        {
            //Act
            var result1 = await Service.GetUserRoleAsync("osminogka@test.com");
            var result2 = await Service.GetUserRoleAsync("redter@test.com");
            var result3 = await Service.GetUserRoleAsync("none@test.com");

            //Assert
            Assert.True(result1.Success);
            Assert.True(result1.Value.isTeacher);
            Assert.False(result1.Value.isStudent);

            Assert.True(result2.Success);
            Assert.False(result2.Value.isTeacher);
            Assert.True(result2.Value.isStudent);

            Assert.True(result3.Success);
            Assert.False(result3.Value.isTeacher);
            Assert.False(result3.Value.isStudent);
        }
    }
}
