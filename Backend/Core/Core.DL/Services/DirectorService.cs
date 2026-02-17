using Microsoft.EntityFrameworkCore;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;

namespace Core.DL.Services
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

            var university = await _universityRepository.Where(obj => obj.Name == model.University && obj.Director.Email == mainUserEmail)
                .Include(obj => obj.Director)
                .FirstOrDefaultAsync();
            if(university == null)
            {
                response.Message = "Unable to do this action";
                return response;
            }

            var entryRequest = await _entryRequestRepository.Where(obj => obj.UniversityId == university.Id && 
                obj.BaseUser.Username == model.Username && obj.SentByUniversity == false)
                .Include(obj => obj.BaseUser)
                .FirstOrDefaultAsync();

            if(entryRequest == null)
            {
                response.Message = "Such request doesn't exist";
                return response;
            }
            await _entryRequestRepository.DeleteAsync(entryRequest);

            var checkForAnotherRequest = await _entryRequestRepository.Where(obj => obj.UniversityId == university.Id && 
                obj.BaseUser.Username == model.Username && obj.SentByUniversity == true)
                .Include(obj => obj.BaseUser)
                .FirstOrDefaultAsync();

            if (checkForAnotherRequest != null)
                await _entryRequestRepository.DeleteAsync(checkForAnotherRequest);

            var user = await _baseUserRepository.Where(obj => obj.Username == model.Username)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();
            if(user == null)
            {
                response.Message = "User not found";
                return response;
            }

            if(user.Universities.Any(u => u.Id == university.Id))
            {
                response.Message = "User already belongs to this university";
                return response;
            }

            university.Members ??= new List<BaseUser>();
            university.Members.Add(user);
            await _universityRepository.UpdateAsync(university);

            response.Success = true;
            response.Message = "User entry request accepted";

            return response;
        }

        public async Task<ResponseMessage> RejectEntryRequestAsync(EntryRequestModel model, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.Where(obj => obj.Name == model.University && obj.Director.Email == mainUserEmail)
                .Include(obj => obj.Director)
                .FirstOrDefaultAsync();
            if (university == null)
            {
                response.Message = "Unable to do this action";
                return response;
            }

            var entryRequest = await _entryRequestRepository.Where(obj => obj.UniversityId == university.Id &&
                obj.BaseUser.Username == model.Username && obj.SentByUniversity == false)
                .Include(obj => obj.BaseUser)
                .FirstOrDefaultAsync();

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

            var university = await _universityRepository.Where(obj => obj.Name == model.Name && obj.Director.Email == email)
                .Include(obj => obj.Director)
                .FirstOrDefaultAsync();
            if(university == null)
            {
                response.Message = "Unable to do this action";
                return response;
            }

            if (!string.IsNullOrEmpty(model.Description))
                university.Description = model.Description;
            if (model.Address != null)
                university.Address = model.Address;

            await _universityRepository.UpdateAsync(university);

            response.Success = true;
            response.Message = "Successfully updated university info";

            return response;
        }
        
        public async Task<ResponseMessage> InviteStudentToUniversityAsync(string universityName, string studentUsername, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var checkIfCanSendRequest = await _universityRepository.Where(obj => obj.Name == universityName && obj.Director.Email == mainUserEmail)
                .Include(obj => obj.Director)
                .FirstOrDefaultAsync();
            if (checkIfCanSendRequest == null)
            {
                response.Message = "You cannot make such action";
                return response;
            }

            var baseUser = await _baseUserRepository.Where(obj => obj.Username == studentUsername && obj.StudentId != null)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();
            if (baseUser == null || baseUser.Universities.Any(u => u.Id == checkIfCanSendRequest.Id))
            {
                response.Message = "Such user doesn't exist or already belongs to this university";
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

            var checkIfCanSendRequest = await _universityRepository.Where(obj => obj.Name == universityName && obj.Director.Email == mainUserEmail)
                .Include(obj => obj.Director)
                .FirstOrDefaultAsync();
            if (checkIfCanSendRequest == null)
            {
                response.Message = "You cannot make such action";
                return response;
            }

            var teacher = await _baseUserRepository.Where(obj => obj.Username == teacherUsername && obj.IsTeacher == true && obj.Teacher.IsVerified == true)
                .Include(obj => obj.Teacher)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();
            if (teacher == null)
            {
                response.Message = "Such user doesn't exist or is not a verified teacher";
                return response;
            }

            if (teacher.Universities.Any(u => u.Id == checkIfCanSendRequest.Id))
            {
                response.Message = "Teacher already belongs to this university";
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
