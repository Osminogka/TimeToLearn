using Users.DAL.SideModels;

namespace Users.DL.Services
{
    public interface IDirectorService
    {
        Task<ResponseMessage> AcceptEntryRequestAsync(EntryRequestModel model, string mainUserEmail);
        Task<ResponseMessage> RejectEntryRequestAsync(EntryRequestModel model, string mainUserEmail);
        Task<ResponseMessage> UpdateUniversityInfoAsync(UpdateUniversityInfoModel model, string email);
    }
}
