using Forums.DAL.Dtos;
using Forums.DAL.Models;
using Forums.DAL.SideModels;
using Forums.DL.Grpc;
using Forums.DL.Repositories;


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

        public async Task<ResponseArray<ReadCommentDto>> GetCommentsAsync(bool isTopic, long recordId)
        {
            ResponseArray<ReadCommentDto> response = new ResponseArray<ReadCommentDto>();

            return response;
        }

        public async Task<ResponseMessage> CreateCommentAsync(CreateCommentDto createCommentDto, string creatorEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var reply = await _grpcClient.GetUserInfoForTopic(createCommentDto.UniversityName, creatorEmail);
            if (!reply.IsAllowed)
                return response;

            dynamic doesRecordExist;

            if (createCommentDto.IsTopic)
                doesRecordExist = await _topicRepository.SingleOrDefaultAsync(obj => obj.Id == createCommentDto.PostId);
            else
                doesRecordExist = await _commentRepository.SingleOrDefaultAsync(obj => obj.Id == createCommentDto.PostId);

            if(doesRecordExist == null)
            {
                response.Message = "Such record doesn't exist";
                return response;
            }

            Comment comment = new Comment()
            {
                CommentContent = createCommentDto.CommentContent,
                CommentCreatorId = reply.UserId,
                IsTopic = createCommentDto.IsTopic,
                PostId = createCommentDto.PostId,
                UniversityOfCreator = reply.UniversityId,
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

            var universityName = await _grpcClient.GetUniversityName(comment.UniversityOfCreator);

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, userEmail);
            if (!reply.IsAllowed)
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
            response.Message = "You liked the topic";

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

            var universityName = await _grpcClient.GetUniversityName(comment.UniversityOfCreator);

            var reply = await _grpcClient.GetUserInfoForTopic(universityName, userEmail);
            if (!reply.IsAllowed)
                return response;

            var isAlreadyDisliked = await _dislikeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == false && obj.PostId == commentId &&
                obj.UserId == reply.UserId);
            if (isAlreadyDisliked != null)
            {
                await _dislikeRepository.DeleteAsync(isAlreadyDisliked);
                response.Success = true;
                response.Message = "You removed your like";
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
            response.Message = "You liked the topic";

            return response;
        }
    }
}
