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
        
        public async Task<ResponseMessage> InviteStudentToUniversityAsync(string universityName, string studentUsername, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var checkIfCanSendRequest = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == universityName && obj.Director.Email == mainUserEmail);
            if (checkIfCanSendRequest == null)
            {
                response.Message = "You cannot make such action";
                return response;
            }

            var baseUser = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Username == studentUsername && obj.StudentId != null && 
                obj.UniversityId != checkIfCanSendRequest.Id);
            if (baseUser == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var doesEntryRequestExist = await _entryRequestRepository.SingleOrDefaultAsync(
                er =>
                    er.BaseUserId == baseUser.Id &&
                    er.UniversityId == checkIfCanSendRequest.Id &&
                    er.SentByUniversity == true);

            if (doesEntryRequestExist != null)
            {
                response.Message = "You already invited this person";
                return response;
            }

            EntryRequest entryRequest = new EntryRequest
            {
                BaseUserId = baseUser.Id,
                UniversityId = checkIfCanSendRequest.Id,
                SentByUniversity = true
            };

            await _entryRequestRepository.AddAsync(entryRequest);

            response.Success = true;
            response.Message = "Invitation is sent";

            return response;
        }
        
        public async Task<ResponseMessage> InviteTeacherToUniversityAsync(string universityName, string teacherUsername, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var checkIfCanSendRequest = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == universityName && obj.Director.Email == mainUserEmail);
            if (checkIfCanSendRequest == null)
            {
                response.Message = "You cannot make such action";
                return response;
            }

            var teacher = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Username == teacherUsername && obj.IsTeacher == true && obj.Teacher.IsVerified == true);
            if (teacher == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var doesEntryRequestExist = await _entryRequestRepository.SingleOrDefaultAsync(
                er =>
                    er.BaseUserId == teacher.Id &&
                    er.UniversityId == checkIfCanSendRequest.Id &&
                    er.SentByUniversity == true);

            if (doesEntryRequestExist != null)
            {
                response.Message = "You already invited this person";
                return response;
            }

            EntryRequest entryRequest = new EntryRequest
            {
                BaseUserId = teacher.Id,
                UniversityId = checkIfCanSendRequest.Id,
                SentByUniversity = true
            };

            await _entryRequestRepository.AddAsync(entryRequest);

            response.Success = true;
            response.Message = "Invitation is sent";

            return response;
        }
    }
}
