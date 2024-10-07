using Forums.DAL.Models;
using Forums.DL.Repositories;
using Forums.DAL.SideModels;
using Forums.DAL.Dtos;

namespace Forums.DL.Services
{
    public class TopicService : ITopicService
    {
        private readonly IBaseRepository<Topic> _topicRepository;

        public TopicService(IBaseRepository<Topic> topicRepository)
        {
            _topicRepository = topicRepository;
        }

        public async Task<ResponseMessage> CreateTopicAsync(CreateTopicDto topic, string creatorEmail)
        {
            ResponseMessage response = new ResponseMessage();

            

            return response;
        }
    }
}
