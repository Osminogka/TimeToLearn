using Users.DAL.Dtos;
using Users.DAL.SideModels;

namespace Users.DL.Services
{
    public interface IBaseUserService
    {
        Task<ResponseGetEnum<string>> GetUsersAsync();
        Task<ResponseWithValue<ReadBaseUserDto>> GetBaseUserAsync(string username);
        Task<ResponseMessage> UpdateUserInfoAsync(UpdateUserInfoModel userInfo, string email);
        Task<ResponseGetEnum<string>> GetInvitesAsync(string email);
        Task<ResponseMessage> AcceptInviteAsync(string universityName, string email);
        Task<ResponseMessage> RejectInviteAsync(string universityName, string email);
    }
}
