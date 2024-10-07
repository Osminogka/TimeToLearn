using Forums.DAL.Dtos;
using Forums.DAL.SideModels;

namespace Forums.DL.Services
{
    public interface ITopicService
    {
        Task<ResponseMessage> CreateTopicAsync(CreateTopicDto topicInfo, string creatorEmail);
        Task<ResponseMessage> LikeTopicAsync(long topicId, string creatorEmail);
        Task<ResponseMessage> DislikeTopicAsync(long topicId, string creatorEmail);
    }
}
