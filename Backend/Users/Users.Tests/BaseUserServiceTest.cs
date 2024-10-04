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
        private IBaseRepository<EntryRequest> EntryRequestRepository { get; set; }

        private BaseUserService Service { get; set; }

        private string UserEmail = "osminogka@test.com";

        public BaseUserServiceTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbBaseUsers"));

            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
            services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

            var serviceProvider = services.BuildServiceProvider();

            var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            var autoMapper = config.CreateMapper();

            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();
            EntryRequestRepository = scopedServices.GetRequiredService<IBaseRepository<EntryRequest>>();

            Service = new BaseUserService(UserRepository, EntryRequestRepository, autoMapper);

            var context = UserRepository.GetContext();
            context.Database.EnsureDeleted();

            var user = new BaseUser()
            {
                Id = 1,
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                Email = "osminogka@test.com",
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

            var university = new University
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
                DirectorId = 2
            };
            context.Add(university);

            context.Add(user);
            context.Add(user2);

            var invite = new EntryRequest
            {
                BaseUserId = 1,
                UniversityId = 1,
                SentByUniversity = true
            };
            context.Add(invite);

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
            var result = await Service.GetBaseUserAsync("Osminogka");

            //Assert
            var response = Assert.IsType<ResponseWithValue<ReadBaseUserDto>>(result);
            Assert.True(response.Success);
            Assert.Equal("Osminogka", response.Value.Username);
        }

        [Fact]
        public async Task UpdateUserInfoTest()
        {
            //Arrange
            UpdateUserInfoModel info = new UpdateUserInfoModel
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

            UpdateUserInfoModel info2 = new UpdateUserInfoModel
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

        [Fact]
        public async Task GetInvitesTest()
        {
            //Act
            var result = await Service.GetInvitesAsync(UserEmail);

            //Assert
            var response = Assert.IsType<ResponseGetEnum<string>>(result);

            Assert.Equal("DKU", response.Enum.First());
            Assert.Single(response.Enum);
        }

        [Fact]
        public async Task AcceptInviteTest()
        {
            //Act
            var result = await Service.AcceptInviteAsync("DKU", UserEmail);

            var user = await UserRepository.SingleOrDefaultAsync(obj => obj.Email == UserEmail);
            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == UserEmail);
            
            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            Assert.True(response.Success);
            Assert.NotNull(user.UniversityId);
            Assert.Null(invite);
        }

        [Fact]
        public async Task RejectInviteTest()
        {
            //Act
            var result = await Service.RejectInviteAsync("DKU", UserEmail);

            var user = await UserRepository.SingleOrDefaultAsync(obj => obj.Email == UserEmail);
            var invite = await EntryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == UserEmail);

            //Assert
            var response = Assert.IsType<ResponseMessage>(result);

            Assert.True(response.Success);
            Assert.Null(user.UniversityId);
            Assert.Null(invite);
        }
    }
}
