using Forums.DAL.Models;
using Forums.DL.Repositories;
using Forums.DAL.SideModels;
using Forums.DAL.Dtos;
using Forums.DL.Grpc;
using Microsoft.EntityFrameworkCore;

namespace Forums.DL.Services
{
    public class TopicService : ITopicService
    {
        private readonly IBaseRepository<Topic> _topicRepository;
        private readonly IBaseRepository<Like> _likeRepository;
        private readonly IBaseRepository<Dislike> _dislikeRepository;
        private readonly IUserInfoClient _grpcClient;

        public TopicService(IBaseRepository<Topic> topicRepository, IBaseRepository<Like> likeRepository,
            IBaseRepository<Dislike> dislikeRepository, IUserInfoClient grpcClient)
        {
            _topicRepository = topicRepository;
            _likeRepository = likeRepository;
            _dislikeRepository = dislikeRepository;
            _grpcClient = grpcClient;
        }

        public async Task<ResponseArray<ReadTopicDto>> GetUniversityTopicsAsync(string universityName, string userEmail, int page)
        {
            ResponseArray<ReadTopicDto> response = new ResponseArray<ReadTopicDto>();
            response.Message = "You don't have such rights";
            const int topicNumbers = 10;

            if (page < 0)
            {
                response.Message = "Invalid page number";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, userEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            var topics = await _topicRepository.Where(obj => obj.UniversityId == reply.UniversityId)
                .Skip(page * topicNumbers).Take(topicNumbers).ToListAsync();

            var creatorIds = topics.Select(t => t.CreatorId).Distinct();
            var nameEntries = await Task.WhenAll(
                creatorIds.Select(async id => (id, name: await _grpcClient.GetUserName(id))));
            var nameMap = nameEntries.ToDictionary(x => x.id, x => x.name);

            response.Success = true;
            response.Message = "You got some topics";
            response.Values = topics.Select(t => new ReadTopicDto
            {
                TopicTitle = t.TopicTitle,
                TopicContent = t.TopicContent,
                CreatorName = nameMap.TryGetValue(t.CreatorId, out var name) ? name : string.Empty,
                Likes = t.LikesOverall,
                Dislikes = t.DislikesOverall
            }).ToList();

            return response;
        }

        public async Task<ResponseMessage> CreateTopicAsync(CreateTopicDto topicInfo, string creatorEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            if (string.IsNullOrWhiteSpace(topicInfo.TopicTitle))
            {
                response.Message = "Topic title cannot be empty";
                return response;
            }

            if (string.IsNullOrWhiteSpace(topicInfo.TopicContent))
            {
                response.Message = "Topic content cannot be empty";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForTopic(topicInfo.UniversityName, creatorEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            Topic topic = new Topic()
            {
                UniversityId = reply.UniversityId,
                CreatorId = reply.UserId,
                TopicContent = topicInfo.TopicContent,
                TopicTitle = topicInfo.TopicTitle,
                CreatedAt = DateTime.UtcNow
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
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, creatorEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            using var tx = await _topicRepository.GetContext().Database.BeginTransactionAsync();

            var isAlreadyLiked = await _likeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == true && obj.PostId == topicId &&
                obj.UserId == reply.UserId);
            if(isAlreadyLiked != null)
            {
                await _likeRepository.DeleteAsync(isAlreadyLiked);
                if (topic.LikesOverall > 0)
                    topic.LikesOverall--;
                await _topicRepository.UpdateAsync(topic);
                await tx.CommitAsync();
                response.Success = true;
                response.Message = "You removed your like";
                return response;
            }

            var isDisliked = await _dislikeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == true && obj.PostId == topicId &&
                obj.UserId == reply.UserId);
            if(isDisliked != null)
            {
                await _dislikeRepository.DeleteAsync(isDisliked);
                if (topic.DislikesOverall > 0)
                    topic.DislikesOverall--;
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
            await tx.CommitAsync();

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
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, creatorEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            using var tx = await _topicRepository.GetContext().Database.BeginTransactionAsync();

            var isAlreadyDisliked = await _dislikeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == true && obj.PostId == topicId &&
                obj.UserId == reply.UserId);
            if (isAlreadyDisliked != null)
            {
                await _dislikeRepository.DeleteAsync(isAlreadyDisliked);
                if (topic.DislikesOverall > 0)
                    topic.DislikesOverall--;
                await _topicRepository.UpdateAsync(topic);
                await tx.CommitAsync();
                response.Success = true;
                response.Message = "You removed your dislike";
                return response;
            }

            var isLiked = await _likeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == true && obj.PostId == topicId &&
                obj.UserId == reply.UserId);
            if(isLiked != null)
            {
                await _likeRepository.DeleteAsync(isLiked);
                if (topic.LikesOverall > 0)
                    topic.LikesOverall--;
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
            await tx.CommitAsync();

            response.Success = true;
            response.Message = "You disliked the topic";

            return response;
        }
    }
}
