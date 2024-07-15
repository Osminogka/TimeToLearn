using Microsoft.EntityFrameworkCore;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;

namespace Users.DL.Services
{
    public class StudentService : IStudentService
    {
        private readonly IBaseRepository<Student> _studentRepository;
        private readonly IBaseRepository<BaseUser> _baseUserRepository;
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<EntryRequest> _entryRequestRepository;

        public StudentService(IBaseRepository<Student> studentRepository, IBaseRepository<BaseUser> baseUserRepository, IBaseRepository<University> universityRepository, IBaseRepository<EntryRequest> entryRequestRepository)
        {
            _studentRepository = studentRepository;
            _baseUserRepository = baseUserRepository;
            _universityRepository = universityRepository;
            _entryRequestRepository = entryRequestRepository;
        }

        public async Task<ResponseMessage> BecomeAStudentAsync(string email)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Email == email);
            if(user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            Student student = new Student
            {
                BaseUserId = user.Id
            };

            await _studentRepository.AddAsync(student);
            user.StudentId = student.Id;

            await _baseUserRepository.UpdateAsync(user);
            response.Success = true;
            response.Message = "You successfully became a student";

            return response;
        }

        public async Task<ResponseMessage> InviteStudentToUniversity(string universityName, string username, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var checkIfCanSendRequest = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == universityName && obj.Director.Email == mainUserEmail);
            if (checkIfCanSendRequest == null)
            {
                response.Message = "You cannot make such action";
                return response;
            }

            var baseUser = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Username == username && obj.IsTeacher == false);
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

        public async Task<ResponseMessage> SendRequestToBecomeStudentOfUniversity(string universityName, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == universityName);
            var mainUser = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Email == mainUserEmail && obj.IsTeacher == false);
            if (mainUser == null || university == null)
                return response;

            if (university.Students.SingleOrDefault(obj => obj.BaseUserId == mainUser.Id) != null)
            {
                response.Message = "You already student of this university";
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

        public async Task<ResponseMessage> EntryUniversityAsync(string universityName, string userEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == universityName);
            var user = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Email == userEmail && obj.IsTeacher == false);
            if (university == null || user == null)
            {
                response.Message = "Such university doesn't exist";
                return response;
            }

            var entryRequestCheck = await _entryRequestRepository.Where(obj => obj.UniversityId == university.Id && obj.BaseUserId == user.Id).ToListAsync();
            if (!university.IsOpened && entryRequestCheck.Where(obj => obj.SentByUniversity == true) == null)
            {
                response.Message = "You are not allowed to entry";
                return response;
            }

            if (entryRequestCheck.Count > 0)
                await _entryRequestRepository.DeleteRangeAsync(entryRequestCheck);

            user.Student!.UniversityId = university.Id;

            return response;
        }
    }
}
