using Forums.DAL.SideModels;

namespace Forums.API.Grpc
{
    public interface IUserInfoClient 
    {
        Task<UserInfoForTopic> GetUserInfoForTopic(string universityName, string userEmail);
        Task<string> GetUniversityName(long universityId);
        Task<string> GetUserName(long userId);
    }
}
