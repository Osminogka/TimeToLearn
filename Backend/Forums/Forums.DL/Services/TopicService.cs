using Forums.DAL.Models;
using Forums.DL.Repositories;
using Forums.DAL.SideModels;
using Forums.DAL.Dtos;
using Forums.DL.Grpc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Forums.DL.Services
{
    public class TopicService : ITopicService
    {
        private readonly IBaseRepository<Topic> _topicRepository;
        private readonly IBaseRepository<Like> _likeRepository;
        private readonly IBaseRepository<Dislike> _dislikeRepository;
        private readonly IUserInfoClient _grpcClient;
        private readonly IMapper _mapper;

        public TopicService(IBaseRepository<Topic> topicRepository, IBaseRepository<Like> likeRepository, 
            IBaseRepository<Dislike> dislikeRepository, IUserInfoClient grpcClient, IMapper mapper)
        {
            _topicRepository = topicRepository;
            _likeRepository = likeRepository;
            _dislikeRepository = dislikeRepository;
            _grpcClient = grpcClient;
            _mapper = mapper;
        }

        public async Task<ResponseArray<ReadTopicDto>> GetUniversityTopicsAsync(string universityName, string userEmail, int page)
        {
            ResponseArray<ReadTopicDto> response = new ResponseArray<ReadTopicDto>();
            response.Message = "You don't have such rights";
            const int topicNumbers = 10; 

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, userEmail);
            if (!reply.IsAllowed)
                return response;

            var topics = await _topicRepository.Where(obj => obj.UniversityId == reply.UniversityId)
                .Skip(page * topicNumbers).Take(topicNumbers).ToListAsync();

            response.Success = true;
            response.Message = "You got some topics";
            response.Values = _mapper.Map<List<ReadTopicDto>>(topics);

            return response;
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
                topic.LikesOverall--;
                await _topicRepository.UpdateAsync(topic);
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
            topic.LikesOverall++;
            await _topicRepository.UpdateAsync(topic);

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
                topic.DislikesOverall--;
                await _topicRepository.UpdateAsync(topic);
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
            topic.DislikesOverall++;
            await _topicRepository.UpdateAsync(topic);

            response.Success = true;
            response.Message = "You liked the topic";

            return response;
        }
    }
}
