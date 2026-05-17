using Forums.DAL.Dtos;
using Forums.DAL.Models;
using Forums.DAL.SideModels;
using Forums.DL.Grpc;
using Forums.DL.Repositories;
using Microsoft.EntityFrameworkCore;


namespace Forums.DL.Services
{
    public class CommentService : ICommentService
    {
        private readonly IBaseRepository<Topic> _topicRepository;
        private readonly IBaseRepository<Comment> _commentRepository;
        private readonly IBaseRepository<Like> _likeRepository;
        private readonly IBaseRepository<Dislike> _dislikeRepository;
        private readonly IUserInfoClient _grpcClient;

        public CommentService(IBaseRepository<Topic> topicRepository, IBaseRepository<Comment> commentRepository, IBaseRepository<Like> likeRepository,
            IBaseRepository<Dislike> dislikeRepository, IUserInfoClient grpcClient)
        {
            _commentRepository = commentRepository;
            _likeRepository = likeRepository;
            _dislikeRepository = dislikeRepository;
            _grpcClient = grpcClient;
            _topicRepository = topicRepository;
        }

        public async Task<ResponseArray<ReadCommentDto>> GetCommentsAsync(bool isTopic, long recordId, string userEmail, int page)
        {
            ResponseArray<ReadCommentDto> response = new ResponseArray<ReadCommentDto>();
            response.Message = "Wrong request";

            if (page < 0)
            {
                response.Message = "Invalid page number";
                return response;
            }

            Record? record = isTopic ?
                await _topicRepository.SingleOrDefaultAsync(obj => obj.Id == recordId) :
                await _commentRepository.SingleOrDefaultAsync(obj => obj.Id == recordId);
            if (record == null)
                return response;

            long universityId = record.UniversityId;

            var universityName = await _grpcClient.GetUniversityName(universityId);
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }
            var reply = await _grpcClient.GetUserInfoForTopic(universityName, userEmail);
            if (reply == null || !reply.IsAllowed)
            {
                response.Message = "You aren't allowed to get comments for this record";
                return response;
            }

            int commentNumber = isTopic ? 10 : 5;
            var comments = await _commentRepository
                .Where(obj => obj.IsTopic == isTopic && obj.PostId == recordId)
                .OrderByDescending(obj => obj.CreatedAt)
                .Skip(page * commentNumber)
                .Take(commentNumber).ToListAsync();

            var nameEntries = await Task.WhenAll(
                comments.Select(c => c.CreatorId).Distinct()
                        .Select(async id => (id, name: await _grpcClient.GetUserName(id))));
            var roleEntries = await Task.WhenAll(
                comments.Select(c => c.CreatorId).Distinct()
                        .Select(async id => (id, role: await _grpcClient.GetUserUniversityRole(id, universityId))));
            var nameMap = nameEntries.ToDictionary(x => x.id, x => x.name);
            var roleMap = roleEntries.ToDictionary(x => x.id, x => x.role);
            var commentIds = comments.Select(c => c.Id).ToList();
            var replyCounts = await _commentRepository.Where(c => !c.IsTopic && commentIds.Contains(c.PostId))
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.LongCount() })
                .ToDictionaryAsync(x => x.PostId, x => x.Count);
            var likeCounts = await _likeRepository.Where(l => !l.IsTopic && commentIds.Contains(l.PostId))
                .GroupBy(l => l.PostId)
                .Select(g => new { PostId = g.Key, Count = g.LongCount() })
                .ToDictionaryAsync(x => x.PostId, x => x.Count);
            var dislikeCounts = await _dislikeRepository.Where(d => !d.IsTopic && commentIds.Contains(d.PostId))
                .GroupBy(d => d.PostId)
                .Select(g => new { PostId = g.Key, Count = g.LongCount() })
                .ToDictionaryAsync(x => x.PostId, x => x.Count);

            foreach (Comment comment in comments)
            {
                ReadCommentDto tempReadCommentDto = new ReadCommentDto();

                tempReadCommentDto.Id = comment.Id;
                tempReadCommentDto.PostId = comment.PostId;
                tempReadCommentDto.IsTopic = comment.IsTopic;
                tempReadCommentDto.CommentContent = comment.CommentContent;
                tempReadCommentDto.CreatedAt = comment.CreatedAt;
                tempReadCommentDto.CreatorName = nameMap.TryGetValue(comment.CreatorId, out var name) ? name : string.Empty;
                tempReadCommentDto.CreatorRole = roleMap.TryGetValue(comment.CreatorId, out var role) ? role ?? "Unknown" : "Unknown";
                tempReadCommentDto.LikesOverall = likeCounts.TryGetValue(comment.Id, out var likeCount) ? likeCount : 0;
                tempReadCommentDto.DislikesOverall = dislikeCounts.TryGetValue(comment.Id, out var dislikeCount) ? dislikeCount : 0;
                tempReadCommentDto.RepliesCount = replyCounts.TryGetValue(comment.Id, out var replyCount) ? replyCount : 0;
                response.Values.Add(tempReadCommentDto);
            }

            response.Success = true;
            response.Message = "You got some comments";

            return response;
        }

        public async Task<ResponseMessage> CreateCommentAsync(CreateCommentDto createCommentDto, string creatorEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            if (string.IsNullOrWhiteSpace(createCommentDto.CommentContent))
            {
                response.Message = "Comment content cannot be empty";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForTopic(createCommentDto.UniversityName, creatorEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            Record? doesRecordExist = createCommentDto.IsTopic ?
                await _topicRepository.SingleOrDefaultAsync(obj => obj.Id == createCommentDto.PostId) :
                await _commentRepository.SingleOrDefaultAsync(obj => obj.Id == createCommentDto.PostId);

            if (doesRecordExist == null)
            {
                response.Message = "Such record doesn't exist";
                return response;
            }

            if (doesRecordExist.UniversityId != reply.UniversityId)
            {
                response.Message = "Such record doesn't exist";
                return response;
            }

            Comment comment = new Comment()
            {
                CommentContent = createCommentDto.CommentContent,
                CreatorId = reply.UserId,
                IsTopic = createCommentDto.IsTopic,
                PostId = createCommentDto.PostId,
                UniversityId = reply.UniversityId,
                CreatedAt = DateTime.UtcNow
            };

            await _commentRepository.AddAsync(comment);

            response.Success = true;
            response.Message = "You created the comment";

            return response;
        }

        public async Task<ResponseMessage> LikeCommentAsync(long commentId, string userEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var comment = await _commentRepository.SingleOrDefaultAsync(obj => obj.Id == commentId);
            if(comment == null)
            {
                response.Message = "Such comment doesn't exist";
                return response;
            }

            var universityName = await _grpcClient.GetUniversityName(comment.UniversityId);
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, userEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            var isAlreadyLiked = await _likeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == false && obj.PostId == commentId &&
                obj.UserId == reply.UserId);
            if (isAlreadyLiked != null)
            {
                await _likeRepository.DeleteAsync(isAlreadyLiked);
                response.Success = true;
                response.Message = "You removed your like";
                return response;
            }

            var isDisliked = await _dislikeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == false && obj.PostId == commentId &&
                obj.UserId == reply.UserId);
            if(isDisliked != null)
            {
                await _dislikeRepository.DeleteAsync(isDisliked);
            }

            Like like = new Like()
            {
                IsTopic = false,
                PostId = commentId,
                UserId = reply.UserId
            };

            await _likeRepository.AddAsync(like);

            response.Success = true;
            response.Message = "You liked the comment";

            return response;
        }

        public async Task<ResponseMessage> DislikeCommentAsync(long commentId, string userEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var comment = await _commentRepository.SingleOrDefaultAsync(obj => obj.Id == commentId);
            if (comment == null)
            {
                response.Message = "Such comment doesn't exist";
                return response;
            }

            var universityName = await _grpcClient.GetUniversityName(comment.UniversityId);
            if (string.IsNullOrEmpty(universityName))
            {
                response.Message = "University not found";
                return response;
            }

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, userEmail);
            if (reply == null || !reply.IsAllowed)
                return response;

            var isAlreadyDisliked = await _dislikeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == false && obj.PostId == commentId &&
                obj.UserId == reply.UserId);
            if (isAlreadyDisliked != null)
            {
                await _dislikeRepository.DeleteAsync(isAlreadyDisliked);
                response.Success = true;
                response.Message = "You removed your dislike";
                return response;
            }

            var isLiked = await _likeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == false && obj.PostId == commentId &&
                obj.UserId == reply.UserId);
            if (isLiked != null)
            {
                await _likeRepository.DeleteAsync(isLiked);
            }

            Dislike dislike = new Dislike()
            {
                IsTopic = false,
                PostId = commentId,
                UserId = reply.UserId
            };

            await _dislikeRepository.AddAsync(dislike);

            response.Success = true;
            response.Message = "You disliked the comment";

            return response;
        }
    }
}
