using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.API.Infrastructure;
using Users.DAL.Context;
using Users.DAL.Dtos;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;
using Users.DL.Services;

namespace Users.Tests
{
    public class BaseUserServiceTest
    {
        private IBaseRepository<BaseUser> UserRepository { get; set; }

        private BaseUserService Service { get; set; }

        private string UserEmail = "osminogka@test.com";

        public BaseUserServiceTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbBaseUsers"));

            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();

            var serviceProvider = services.BuildServiceProvider();

            var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            var autoMapper = config.CreateMapper();

            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();

            Service = new BaseUserService(UserRepository, autoMapper);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            var user = new BaseUser()
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
                UniversityId = 1,
                IsTeacher = true
            };

            var user2 = new BaseUser
            {
                Id = 2,
                OriginalId = Guid.NewGuid(),
                Username = "Redter",
                Email = "redter@test.com",
                UniversityId = 1,
                IsTeacher = true
            };

            context.Add(user);
            context.Add(user2);

            context.SaveChanges();
        }

        [Fact]
        public async Task GetUsersTest()
        {
            //Act
            var result = await Service.GetUsersAsync();

            //Assert
            var response = Assert.IsType<ResponseGetEnum<string>>(result);
            Assert.True(response.Success);
            Assert.Equal(2, response.Enum.Count());
        }

        [Fact]
        public async Task GetBaseUserTest()
        {
            //Act
            var result = await Service.GetBaseUserAsync(UserEmail);

            //Assert
            var response = Assert.IsType<ResponseWithValue<ReadBaseUserDto>>(result);
            Assert.True(response.Success);
            Assert.Equal("Osminogka", response.Value.Username);
        }

        [Fact]
        public async Task UpdateUserInfoTest()
        {
            //Arrange
            UpdateUserInfo info = new UpdateUserInfo
            {
                FirstName = "Peter",
                LastName = "Parker",
                Address = new Address
                {
                    Country = "USA",
                    City = "New York",
                    Street = "Daun street"
                },
                Phone = "77777777777"
            };

            UpdateUserInfo info2 = new UpdateUserInfo
            {
                FirstName = "Peter",
                LastName = "Parker",
                Address = new Address
                {
                    Country = "Germany",
                    City = null,
                    Street = null
                },
                Phone = "77777777777"
            };

            //Act
            var result = await Service.UpdateUserInfoAsync(info, UserEmail);

            var user = await UserRepository.SingleOrDefaultAsync(obj => obj.Email == UserEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);
            Assert.True(response.Success);
            Assert.Equal("USA", user.Address.Country);

            //Act
            await Service.UpdateUserInfoAsync(info2, UserEmail);

            var user2 = await UserRepository.SingleOrDefaultAsync(obj => obj.Email == UserEmail);

            //Assert
            Assert.Equal("Germany", user2.Address.Country);
            Assert.Null(user2.Address.City);
        }
    }
}
