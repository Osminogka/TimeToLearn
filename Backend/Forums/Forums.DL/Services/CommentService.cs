using Forums.DAL.Models;
using Forums.DAL.SideModels;
using Forums.DL.Grpc;
using Forums.DL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<ResponseMessage> CreateCommentAsync(CommentInfo commentInfo,string commentContent, string creatorEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var reply = await _grpcClient.GetUserInfoForTopic(commentInfo.UniversityName, creatorEmail);
            if (!reply.IsAllowed)
                return response;

            dynamic doesRecordExist;

            if (commentInfo.IsTopic)
                doesRecordExist = await _topicRepository.SingleOrDefaultAsync(obj => obj.Id == commentInfo.PostId);
            else
                doesRecordExist = await _commentRepository.SingleOrDefaultAsync(obj => obj.Id == commentInfo.PostId);

            if(doesRecordExist == null)
            {
                response.Message = "Such record doesn't exist";
                return response;
            }

            Comment comment = new Comment()
            {
                CommentContent = commentContent,
                CommentCreatorId = reply.UserId,
                IsTopic = commentInfo.IsTopic,
                PostId = commentInfo.PostId
            };

            await _commentRepository.AddAsync(comment);

            response.Success = true;
            response.Message = "You created the comment";

            return response;
        }

        public async Task<ResponseMessage> LikeCommentAsync(CommentInfo commentInfo, string userEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var reply = await _grpcClient.GetUserInfoForTopic(commentInfo.UniversityName, userEmail);
            if (!reply.IsAllowed)
                return response;

            var isAlreadyLiked = await _likeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == false && obj.PostId == commentInfo.OriginalId &&
                obj.UserId == reply.UserId);
            if (isAlreadyLiked != null)
            {
                await _likeRepository.DeleteAsync(isAlreadyLiked);
                response.Success = true;
                response.Message = "You removed your like";
                return response;
            }

            Like like = new Like()
            {
                IsTopic = false,
                PostId = commentInfo.OriginalId,
                UserId = reply.UserId
            };

            await _likeRepository.AddAsync(like);

            response.Success = true;
            response.Message = "You liked the topic";

            return response;
        }

        public async Task<ResponseMessage> DislikeCommentAsync(CommentInfo commentInfo, string userEmail)
        {
            ResponseMessage response = new ResponseMessage();
            response.Message = "You don't have such rights";

            var reply = await _grpcClient.GetUserInfoForTopic(commentInfo.UniversityName, userEmail);
            if (!reply.IsAllowed)
                return response;

            var isAlreadyDisliked = await _dislikeRepository.SingleOrDefaultAsync(obj => obj.IsTopic == false && obj.PostId == commentInfo.OriginalId &&
                obj.UserId == reply.UserId);
            if (isAlreadyDisliked != null)
            {
                await _dislikeRepository.DeleteAsync(isAlreadyDisliked);
                response.Success = true;
                response.Message = "You removed your like";
                return response;
            }

            Dislike dislike = new Dislike()
            {
                IsTopic = false,
                PostId = commentInfo.OriginalId,
                UserId = reply.UserId
            };

            await _dislikeRepository.AddAsync(dislike);

            response.Success = true;
            response.Message = "You liked the topic";

            return response;
        }
    }
}
