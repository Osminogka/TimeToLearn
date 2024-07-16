using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Users.API.EventProcessing;
using Users.API.Infrastructure;
using Users.DAL.Context;
using Users.DAL.Dtos;
using Users.DAL.Models;
using Users.DL.Repositories;
using Users.DL.Services;

namespace Users.Tests
{
    public class MessageBusTests
    {
        private IEventProcessor EventProcessor;
        private ServiceProvider ServiceProvider;

        private IBaseRepository<BaseUser> UserRepository;

        public MessageBusTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDbBus"));

            services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();

            ServiceProvider = services.BuildServiceProvider();

            var scope = ServiceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            var autoMapper = config.CreateMapper();

            UserRepository = scopedServices.GetRequiredService<IBaseRepository<BaseUser>>();

            EventProcessor = new EventProcessor(scopedServices.GetRequiredService<IServiceScopeFactory>(), autoMapper);
        }

        [Fact]
        public async Task ProcessEventPublishBaseUserTest()
        {
            var testUserPublishDto = new BaseUserPublishDto
            {
                Email = "osminogka@test@com",
                OriginalId = Guid.NewGuid(),
                Username = "Osminogka",
                PhoneNumber = "87051451870",
                Event = "BaseUser_Published"
            };

            var message = JsonSerializer.Serialize(testUserPublishDto);

            await EventProcessor.ProcessEvent(message);

            var result = await UserRepository.SingleOrDefaultAsync(obj => obj.Email == testUserPublishDto.Email);

            Assert.Equal("Osminogka", result.Username);
        }
    }
}
