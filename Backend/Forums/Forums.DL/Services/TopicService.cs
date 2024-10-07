using Forums.DAL.Models;
using Forums.DL.Repositories;
using Forums.DAL.SideModels;
using Forums.DAL.Dtos;
using Forums.DL.Grpc;

namespace Forums.DL.Services
{
    public class TopicService : ITopicService
    {
        private readonly IBaseRepository<Topic> _topicRepository;
        private readonly IUserInfoClient _grpcClient;
        private readonly IBaseRepository<Like> _likeRepository;
        private readonly IBaseRepository<Dislike> _dislikeRepository;

        public TopicService(IBaseRepository<Topic> topicRepository, IBaseRepository<Like> likeRepository, 
            IBaseRepository<Dislike> dislikeRepository, IUserInfoClient grpcClient)
        {
            _topicRepository = topicRepository;
            _grpcClient = grpcClient;
            _likeRepository = likeRepository;
            _dislikeRepository = dislikeRepository;
        }

        public async Task<ResponseMessage> CreateTopicAsync(CreateTopicDto topicInfo, string creatorEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var reply = await _grpcClient.GetUserInfoForTopic(topicInfo.UniversityName, creatorEmail);
            if (!reply.IsAllowed)
                return response;

            Topic topic = new Topic()
            {
                UniversityId = reply.UniversityId,
                TopicCreatorId = reply.UserId,
                TopicContent = topicInfo.TopicContent,
                TopicTitle = topicInfo.TopicTitle,
            };

            await _topicRepository.AddAsync(topic);
            response.Success = true;
            response.Message = "Topic created";

            return response;
        }

        public async Task<ResponseMessage> LikeTopicAsync(long topicId, string creatorEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var topic = await _topicRepository.SingleOrDefaultAsync(obj => obj.Id == topicId);
            if(topic == null)
            {
                response.Message = "Such topic doesn't exist";
                return response;
            }
            var universityName = await _grpcClient.GetUniversityName(topic.UniversityId);

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, creatorEmail);
            if (!reply.IsAllowed)
                return response;

            var isAlreadyLiked = await _likeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == true && obj.PostId == topicId &&
                obj.UserId == reply.UserId);
            if(isAlreadyLiked != null)
            {
                await _likeRepository.DeleteAsync(isAlreadyLiked);
                response.Success = true;
                response.Message = "You removed your like";
                return response;
            }

            Like like = new Like()
            {
                IsTopic = true,
                PostId = topicId,
                UserId = reply.UserId
            };

            await _likeRepository.AddAsync(like);

            response.Success = true;
            response.Message = "You liked the topic";

            return response;
        }

        public async Task<ResponseMessage> DislikeTopicAsync(long topicId, string creatorEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var topic = await _topicRepository.SingleOrDefaultAsync(obj => obj.Id == topicId);
            if (topic == null)
            {
                response.Message = "Such topic doesn't exist";
                return response;
            }
            var universityName = await _grpcClient.GetUniversityName(topic.UniversityId);

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, creatorEmail);
            if (!reply.IsAllowed)
                return response;

            var isAlreadyDisliked = await _dislikeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == true && obj.PostId == topicId &&
                obj.UserId == reply.UserId);
            if (isAlreadyDisliked != null)
            {
                await _dislikeRepository.DeleteAsync(isAlreadyDisliked);
                response.Success = true;
                response.Message = "You removed your dislike";
                return response;
            }

            Dislike dislike = new Dislike()
            {
                IsTopic = true,
                PostId = topicId,
                UserId = reply.UserId
            };

            await _dislikeRepository.AddAsync(dislike);

            response.Success = true;
            response.Message = "You liked the topic";

            return response;
        }
    }
}
