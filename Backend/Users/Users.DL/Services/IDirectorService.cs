using Users.DAL.SideModels;

namespace Users.DL.Services
{
    public interface IDirectorService
    {
        Task<ResponseMessage> AcceptEntryRequestAsync(EntryRequestModel model, string mainUserEmail);
        Task<ResponseMessage> RejectEntryRequestAsync(EntryRequestModel model, string mainUserEmail);
        Task<ResponseMessage> UpdateUniversityInfoAsync(UpdateUniversityInfoModel model, string email);
        Task<ResponseMessage> InviteStudentToUniversityAsync(string universityName, string studentUsername,
            string mainUserEmail);

        Task<ResponseMessage> InviteTeacherToUniversityAsync(string universityName, string teacherUsername,
            string mainUserEmail);
    }
}
