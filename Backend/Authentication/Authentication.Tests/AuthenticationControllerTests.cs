using Authentication.API.AsyncDataService;
using Authentication.API.Controllers;
using Authentication.DAL.Dtos;
using Authentication.DAL.Models;
using Authentication.DL.Repositories;
using Authentication.DL.Services;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Authentication.Tests
{
    public class AuthenticationControllerTests
    {
        private Mock<IUsersRepository> Repository { get; set; }

        private AuthenticationController Controller { get; set; }

        public AuthenticationControllerTests()
        {
            var users = new List<AppUser>
            {
                new AppUser
                {
                    UserName = "Test",
                    Id = Guid.NewGuid().ToString(),
                    Email = "test@test.test"
                }

            };

            BaseUserPublishDto userDto = new BaseUserPublishDto
            {
                Email = "osminogka@test.test",
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
            };

            var list = new List<IdentityRole>()
            {
                new IdentityRole("Teacher"),
                new IdentityRole("Student")
            }.AsQueryable();

            var claims = new List<Claim>()
            {
                new Claim("AccountType", Roles.Teacher),
                new Claim("University", "DKU"),
                new Claim("Degree", "Noob")
            };

            var roles = new List<string>()
            {
                "Student"
            };

            Repository = new Mock<IUsersRepository>();

            Repository.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string email) => users.SingleOrDefault(obj => obj.Email == email));

            Repository.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.Run(() => Microsoft.AspNetCore.Identity.SignInResult.Success));

            Repository.Setup(x => x.SetUserRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .Returns(Task.Run(() => IdentityResult.Success));

            Repository.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<AppUser, string>((x, y) => users.Add(x));

            Repository.Setup(x => x.GetAllClaimsAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(claims);

            Repository.Setup(x => x.AddClaimAsync(It.IsAny<AppUser>(), It.IsAny<Claim>()))
                .Callback((AppUser user, Claim claim) => Task.Run(() => claims.Add(claim)))
                .ReturnsAsync(IdentityResult.Success);

            Repository.Setup(x => x.GetUserRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(roles);

            var authService = new AuthService(Repository.Object, GetTestConfiguration());
            var logger = new Mock<ILogger<AuthenticationController>>();

            var autoMapper = new Mock<IMapper>();
            autoMapper.Setup(x => x.Map<BaseUserPublishDto>(It.IsAny<AppUser>()))
                .Returns(userDto);


            var messageBus = new Mock<IMessageBusClient>();
            messageBus.Setup(x => x.PublishNewUser(It.IsAny<BaseUserPublishDto>()));

            Controller = new AuthenticationController(authService, Repository.Object, messageBus.Object ,autoMapper.Object ,logger.Object);
        }

        [Fact]
        public async Task CanUserRegister()
        {
            //Arrange
            RegisterRequestModel model = new RegisterRequestModel
            {
                Email = "osminogka@test.test",
                Name = "Osminogka",
                Password = "Test123!"
            };

            //Act
            var result = await Controller.RegisterAsync(model);

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseMessage>(okResult.Value);

            Assert.Equal(true, response.Success);
        }

        [Fact]
        public async Task CanUserLogin()
        {
            //Arrange
            LoginRequestModel model = new LoginRequestModel
            {
                Email = "test@test.test",
                Password = "Test123!"
            };

            //Act
            var result = await Controller.LoginAsync(model);

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ResponseMessage>(okResult.Value);

            Assert.Equal(true, response.Success);
        }

        private IConfiguration GetTestConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string> {
            {"Jwt:Key", "EUt719k5GENP1pWWhrmyDldHPaKXyIa9yImWhPuqHBUlgZ10Fk"},
            // Add more settings as needed
        };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            return configuration;
        }
    }
}
