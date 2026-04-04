using Microsoft.Extensions.DependencyInjection;
using Core.DAL.Context;
using Core.DAL.Models;
using Core.DL.Repositories;
using Core.DL.Services;

namespace Users.Tests
{
    public class GeneralUserInfoServiceTest
    {
        private IBaseRepository<BaseUser> UserRepository { get; set; }

        private GeneralUserInfoService Service { get; set; }

        public GeneralUserInfoServiceTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbGeneralInfo"));

            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();

            var serviceProvider = services.BuildServiceProvider();

            var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();

            Service = new GeneralUserInfoService(UserRepository);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            // Teacher
            var teacher = new BaseUser()
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
                TeacherId = 1
            };

            // Implicit student — no TeacherId
            var student = new BaseUser
            {
                Id = 2,
                OriginalId = Guid.NewGuid(),
                Username = "Redter",
                Email = "redter@test.com",
            };

            context.Add(teacher);
            context.Add(student);

            context.SaveChanges();
        }

        [Fact]
        public async Task GetRoleTest()
        {
            var result1 = await Service.GetUserRoleAsync("osminogka@test.com");
            var result2 = await Service.GetUserRoleAsync("redter@test.com");

            Assert.True(result1.Success);
            Assert.True(result1.Value.isTeacher);
            Assert.False(result1.Value.isStudent);

            Assert.True(result2.Success);
            Assert.False(result2.Value.isTeacher);
            Assert.True(result2.Value.isStudent);
        }
    }
}
