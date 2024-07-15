using AutoMapper;
using System.Text.Json;
using Users.DAL.Dtos;
using Users.DAL.Models;
using Users.DL.Repositories;

namespace Users.API.EventProcessing
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public EventProcessor(IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }

        public async Task ProcessEvent(string message)
        {
            var eventType = DetermineEvent(message);

            switch (eventType)
            {
                case EventType.BaseUserPublished:
                    await addBaseUser(message);
                    break;
                default:
                    break;
            }
        }

        private EventType DetermineEvent(string notifcationMessage)
        {
            Console.WriteLine("--> Determining Event");

            var eventType = JsonSerializer.Deserialize<GenericEventDto>(notifcationMessage);

            switch (eventType.Event)
            {
                case "BaseUser_Published":
                    Console.WriteLine("--> BaseUser Published Event Detected");
                    return EventType.BaseUserPublished;
                default:
                    Console.WriteLine("--> Could not determine the event type");
                    return EventType.Undetermined;
            }
        }

        private async Task addBaseUser(string platformPublishedMessage)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IBaseRepository<BaseUser>>();

                var baseUserPublishedDto = JsonSerializer.Deserialize<BaseUserPublishDto>(platformPublishedMessage);

                try
                {
                    var user = _mapper.Map<BaseUser>(baseUserPublishedDto);
                    var baseUserExist = await repo.SingleOrDefaultAsync(obj => obj.OriginalId == baseUserPublishedDto.OriginalId);
                    if (baseUserExist == null)
                    {
                        await repo.AddAsync(user);
                        Console.WriteLine("--> BaseUser added!");
                    }
                    else
                    {
                        Console.WriteLine("--> BaseUser already exists...");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--> Could not add BaseUser to DB {ex.Message}");
                }
            }
        }
    }

    enum EventType
    {
        BaseUserPublished,
        Undetermined
    }
}
