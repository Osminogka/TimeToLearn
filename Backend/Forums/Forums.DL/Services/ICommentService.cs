using Forums.DAL.Dtos;
using Forums.DAL.SideModels;

namespace Forums.DL.Services
{
    public interface ICommentService
    {
        Task<ResponseArray<ReadCommentDto>> GetCommentsAsync(bool isTopic, long recordId, string userEmail, int page);
        Task<ResponseMessage> CreateCommentAsync(CreateCommentDto createCommentDto, string creatorEmail);
        Task<ResponseMessage> LikeCommentAsync(long commentId, string userEmail);
        Task<ResponseMessage> DislikeCommentAsync(long commentId, string userEmail);
    }
}
