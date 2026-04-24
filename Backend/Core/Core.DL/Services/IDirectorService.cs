using Core.DAL.SideModels;

namespace Core.DL.Services
{
    public interface IDirectorService
    {
        Task<ResponseMessage> UpdateUniversityInfoAsync(UpdateUniversityInfoModel model, string email);
        Task<ResponseMessage> InviteStudentToUniversityAsync(string universityName, string studentUsername,
            string mainUserEmail);

        Task<ResponseMessage> InviteTeacherToUniversityAsync(string universityName, string teacherUsername,
            string mainUserEmail);

        Task<ResponseMessage> RemoveMemberFromUniversityAsync(EntryRequestModel model, string mainUserEmail);
    }
}
