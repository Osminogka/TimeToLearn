using Authentication.API.Controllers;
using Authentication.DAL.Models;
using Authentication.DL.Repositories;
using Authentication.DL.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Authentication.Tests.Controllers
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

            }.AsQueryable();

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
                .Returns((string email) => Task.Run(() => users.SingleOrDefault(obj => obj.Email == email)));

            Repository.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.Run(() => Microsoft.AspNetCore.Identity.SignInResult.Success));

            Repository.Setup(x => x.SetUserRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .Returns(Task.Run(() => IdentityResult.Success));

            Repository.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .Callback((AppUser user, string password) => Task.Run(() => users.Append(user)))
                .ReturnsAsync(IdentityResult.Success);

            Repository.Setup(x => x.GetAllClaimsAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(claims);

            Repository.Setup(x => x.AddClaimAsync(It.IsAny<AppUser>(), It.IsAny<Claim>()))
                .Callback((AppUser user, Claim claim) => Task.Run(() => claims.Add(claim)))
                .ReturnsAsync(IdentityResult.Success);

            Repository.Setup(x => x.GetUserRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(roles);

            var authService = new AuthService(Repository.Object, GetTestConfiguration());
            var mock = new Mock<ILogger<AuthenticationController>>();
            ILogger<AuthenticationController> logger = mock.Object;
            Controller = new AuthenticationController(authService, Repository.Object, logger);
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
