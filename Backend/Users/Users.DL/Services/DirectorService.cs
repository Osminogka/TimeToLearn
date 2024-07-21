using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;

namespace Users.DL.Services
{
    public class DirectorService : IDirectorService
    {
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<EntryRequest> _entryRequestRepository;
        private readonly IBaseRepository<BaseUser> _baseUserRepository;

        public DirectorService(IBaseRepository<University> universityRepository, IBaseRepository<EntryRequest> entryRequestRepository, IBaseRepository<BaseUser> baseUserRepository)
        {
            _universityRepository = universityRepository;
            _entryRequestRepository = entryRequestRepository;
            _baseUserRepository = baseUserRepository;
        }

        public async Task<ResponseMessage> AcceptEntryRequestAsync(EntryRequestModel model, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == model.University && obj.Director.Email == mainUserEmail);
            if(university == null)
            {
                response.Message = "Unable to do this action";
                return response;
            }

            var entryRequest = await _entryRequestRepository.SingleOrDefaultAsync(obj => obj.UniversityId == university.Id && 
                obj.BaseUser.Username == model.Username && obj.SentByUniversity == false);

            if(entryRequest == null)
            {
                response.Message = "Such request doesn't exist";
                return response;
            }
            await _entryRequestRepository.DeleteAsync(entryRequest);

            var checkForAnotherRequest = await _entryRequestRepository.SingleOrDefaultAsync(obj => obj.UniversityId == university.Id && 
                obj.BaseUser.Username == model.Username && obj.SentByUniversity == true);

            if (checkForAnotherRequest != null)
                await _entryRequestRepository.DeleteAsync(checkForAnotherRequest);

            var user = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Username == model.Username);
            user!.UniversityId = university.Id;

            await _baseUserRepository.UpdateAsync(user);

            response.Success = true;
            response.Message = "User entry request accepted";

            return response;
        }

        public async Task<ResponseMessage> RejectEntryRequestAsync(EntryRequestModel model, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == model.University && obj.Director.Email == mainUserEmail);
            if (university == null)
            {
                response.Message = "Unable to do this action";
                return response;
            }

            var entryRequest = await _entryRequestRepository.SingleOrDefaultAsync(obj => obj.UniversityId == university.Id &&
                obj.BaseUser.Username == model.Username && obj.SentByUniversity == false);

            if (entryRequest == null)
            {
                response.Message = "Such request doesn't exist";
                return response;
            }
            await _entryRequestRepository.DeleteAsync(entryRequest);

            response.Success = true;
            response.Message = "User entry request rejected";

            return response;
        }

        public async Task<ResponseMessage> UpdateUniversityInfoAsync(UpdateUniversityInfoModel model, string email)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == model.Name && obj.Director.Email == email);
            if(university == null)
            {
                response.Message = "Unable to do this action";
                return response;
            }

            university.Description = string.IsNullOrEmpty(model.Description) ? university.Description : model.Description;
            university.Address = model.Address;

            await _universityRepository.UpdateAsync(university);

            response.Success = true;
            response.Message = "Successfully updated university info";

            return response;
        }
    }
}
