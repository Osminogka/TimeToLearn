using Users.DAL.Dtos;
using Users.DAL.SideModels;

namespace Users.DL.Services
{
    public interface IBaseUserService
    {
        Task<ResponseGetEnum<string>> GetUsersAsync();
        Task<ResponseWithValue<ReadBaseUserDto>> GetBaseUserAsync(string email);
        Task<ResponseMessage> UpdateUserInfoAsync(UpdateUserInfo userInfo, string email);
    }
}
