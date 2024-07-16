using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;

namespace Users.DL.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly IBaseRepository<Teacher> _teacherRepository;
        private readonly IBaseRepository<BaseUser> _baseUserRepository;
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<EntryRequest> _entryRequestRepository;

        public TeacherService(IBaseRepository<Teacher> teacherRepository, IBaseRepository<BaseUser> baseUserRepository, IBaseRepository<University> universityRepository, IBaseRepository<EntryRequest> entryRequestRepository)
        {
            _teacherRepository = teacherRepository;
            _baseUserRepository = baseUserRepository;
            _universityRepository = universityRepository;
            _entryRequestRepository = entryRequestRepository;
        }

        public async Task<ResponseMessage> InviteTeacherToUniversity(string universityName, string username, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var checkIfCanSendRequest = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == universityName && obj.Director.Email == mainUserEmail);
            if (checkIfCanSendRequest == null)
            {
                response.Message = "You cannot make such action";
                return response;
            }

            var baseUser = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Username == username && obj.IsTeacher == true);
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

        public async Task<ResponseMessage> SendRequestToBecomeTeacherOfUniversity(string universityName, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == universityName);
            var mainUser = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Email == mainUserEmail && obj.IsTeacher == true);
            if (mainUser == null || university == null)
                return response;
            
            if (university.Members.SingleOrDefault(obj => obj.Id == mainUser.Id) != null)
            {
                response.Message = "You already teacher of this university";
                return response;
            }

            var doesEntryRequestExist = await _entryRequestRepository.SingleOrDefaultAsync(
                er =>
                    er.BaseUserId == mainUser.Id &&
                    er.UniversityId == university.Id &&
                    er.SentByUniversity == false);

            if (doesEntryRequestExist != null)
            {
                response.Message = "You already sent entry request to this university";
                return response;
            }

            EntryRequest entryRequest = new EntryRequest
            {
                BaseUserId = university.Id,
                UniversityId = mainUser.Id,
                SentByUniversity = false
            };

            await _entryRequestRepository.AddAsync(entryRequest);

            response.Success = true;
            response.Message = "Request is sent";

            return response;
        }
    }
}
