using Courses.DAL.SideModels;

namespace Courses.DL.Grpc
{
    public interface IUserInfoClient 
    {
        Task<UserInfoForCourse> GetUserInfoForCourse(string universityName, string userEmail);
        Task<string> GetUniversityName(long universityId);
        Task<string> GetUserName(long userId);
    }
}
