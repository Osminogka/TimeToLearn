using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Core.DAL.Dtos;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;

namespace Core.DL.Services
{
    public class BaseUserService : IBaseUserService
    {
        private readonly IBaseRepository<BaseUser> _baseUserRepository;
        private readonly IBaseRepository<EntryRequest> _entryRequestRepository;
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<StudentEnrollment> _studentEnrollmentRepository;
        private readonly IBaseRepository<TeacherEnrollment> _teacherEnrollmentRepository;
        private readonly IMapper _mapper;

        public BaseUserService(
            IBaseRepository<BaseUser> baseUserRepository,
            IBaseRepository<EntryRequest> entryRequestRepository,
            IBaseRepository<University> universityRepository,
            IBaseRepository<StudentEnrollment> studentEnrollmentRepository,
            IBaseRepository<TeacherEnrollment> teacherEnrollmentRepository,
            IMapper mapper)
        {
            _baseUserRepository = baseUserRepository;
            _entryRequestRepository = entryRequestRepository;
            _universityRepository = universityRepository;
            _studentEnrollmentRepository = studentEnrollmentRepository;
            _teacherEnrollmentRepository = teacherEnrollmentRepository;
            _mapper = mapper;
        }

        public async Task<ResponseGetEnum<string>> GetUsersAsync()
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();

            var users = await _baseUserRepository.GetAllAsync();

            response.Success = true;
            response.Message = "Got all users";
            response.Enum = users.Select(obj => obj.Username).ToList();

            return response;
        }

        public async Task<ResponseWithValue<ReadBaseUserDto>> GetBaseUserAsync(string username)
        {
            ResponseWithValue<ReadBaseUserDto> response = new ResponseWithValue<ReadBaseUserDto>();

            var user = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Username == username);
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            response.Success = true;
            response.Message = "Got user";
            response.Value = _mapper.Map<ReadBaseUserDto>(user);

            return response;
        }

        public async Task<ResponseMessage> UpdateUserInfoAsync(UpdateUserInfoModel userInfo, string email)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Email == email);
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            if (userInfo.FirstName != null)
                user.FirstName = userInfo.FirstName;
            if (userInfo.LastName != null)
                user.LastName = userInfo.LastName;
            if (userInfo.Phone != null)
                user.PhoneNumber = userInfo.Phone;
            if (userInfo.Address != null)
                user.Address = userInfo.Address;

            await _baseUserRepository.UpdateAsync(user);

            response.Success = true;
            response.Message = "Updated successfully";

            return response;
        }

        public async Task<ResponseGetEnum<string>> GetInvitesAsync(string email)
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();

            var user = await _baseUserRepository.Where(obj => obj.Email == email)
                .Include(obj => obj.EntryRequests)
                .ThenInclude(obj => obj.University)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var studentUniversityIds = await _studentEnrollmentRepository
                .Where(e => e.BaseUserId == user.Id)
                .Select(e => e.UniversityId)
                .ToListAsync();
            var teacherUniversityIds = await _teacherEnrollmentRepository
                .Where(e => e.BaseUserId == user.Id)
                .Select(e => e.UniversityId)
                .ToListAsync();
            var memberUniversityIds = studentUniversityIds.Union(teacherUniversityIds).ToHashSet();

            response.Enum = user.EntryRequests
                .Where(er => er.SentByUniversity && !memberUniversityIds.Contains(er.UniversityId))
                .Select(obj => obj.University.Name);
            response.Success = true;
            response.Message = "Got invites of user";

            return response;
        }

        public async Task<ResponseMessage> AcceptInviteAsync(string universityName, string email)
        {
            ResponseMessage response = new ResponseMessage();

            var doesRequestExist = await _entryRequestRepository.Where(obj => obj.BaseUser.Email == email && obj.University.Name == universityName && obj.SentByUniversity == true)
                .Include(obj => obj.BaseUser)
                .Include(obj => obj.University)
                .FirstOrDefaultAsync();

            if (doesRequestExist == null)
            {
                response.Message = "Such invite doesn't exist";
                return response;
            }

            var user = await _baseUserRepository.Where(obj => obj.Email == email)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "User not found";
                return response;
            }

            var checkForAnotherRequest = await _entryRequestRepository.Where(obj => obj.BaseUserId == user.Id && obj.University.Name == universityName && obj.SentByUniversity == false)
                .Include(obj => obj.University)
                .FirstOrDefaultAsync();
            if (checkForAnotherRequest != null)
                await _entryRequestRepository.DeleteAsync(checkForAnotherRequest);

            if (user.TeacherId != null)
            {
                if (!doesRequestExist.InviteAsTeacher)
                {
                    response.Message = "This invite allows joining as student only";
                    return response;
                }

                var existing = await _teacherEnrollmentRepository.SingleOrDefaultAsync(
                    e => e.BaseUserId == user.Id && e.UniversityId == doesRequestExist.UniversityId);
                if (existing != null)
                {
                    response.Message = "User already belongs to this university";
                    return response;
                }
                await _teacherEnrollmentRepository.AddAsync(new TeacherEnrollment
                {
                    BaseUserId = user.Id,
                    UniversityId = doesRequestExist.UniversityId
                });
            }
            else
            {
                if (doesRequestExist.InviteAsTeacher)
                {
                    response.Message = "This invite allows joining as teacher only";
                    return response;
                }

                var existing = await _studentEnrollmentRepository.SingleOrDefaultAsync(
                    e => e.BaseUserId == user.Id && e.UniversityId == doesRequestExist.UniversityId);
                if (existing != null)
                {
                    response.Message = "User already belongs to this university";
                    return response;
                }
                await _studentEnrollmentRepository.AddAsync(new StudentEnrollment
                {
                    BaseUserId = user.Id,
                    UniversityId = doesRequestExist.UniversityId
                });
            }

            await _entryRequestRepository.DeleteAsync(doesRequestExist);

            response.Success = true;
            response.Message = "Invite accepted";

            return response;
        }

        public async Task<ResponseMessage> RejectInviteAsync(string universityName, string email)
        {
            ResponseMessage response = new ResponseMessage();

            var doesRequestExist = await _entryRequestRepository.Where(obj => obj.BaseUser.Email == email && obj.University.Name == universityName && obj.SentByUniversity == true)
                .Include(obj => obj.BaseUser)
                .Include(obj => obj.University)
                .FirstOrDefaultAsync();

            if (doesRequestExist == null)
            {
                response.Message = "Such invite doesn't exist";
                return response;
            }
            await _entryRequestRepository.DeleteAsync(doesRequestExist);

            response.Success = true;
            response.Message = "Invite rejected";

            return response;
        }

        public async Task<ResponseMessage> LeaveUniversityAsync(string universityName, string email)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _baseUserRepository.Where(obj => obj.Email == email)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "User not found";
                return response;
            }

            var isDirector = await _universityRepository.Where(u => u.Name == universityName && u.DirectorId == user.Id).AnyAsync();
            if (isDirector)
            {
                response.Message = "You cannot leave a university you are directing. Transfer directorship first";
                return response;
            }

            var studentEnrollment = await _studentEnrollmentRepository.Where(
                e => e.BaseUserId == user.Id && e.University.Name == universityName)
                .FirstOrDefaultAsync();
            var teacherEnrollment = await _teacherEnrollmentRepository.Where(
                e => e.BaseUserId == user.Id && e.University.Name == universityName)
                .FirstOrDefaultAsync();

            if (studentEnrollment == null && teacherEnrollment == null)
            {
                response.Message = "You are not a member of this university";
                return response;
            }

            if (studentEnrollment != null)
                await _studentEnrollmentRepository.DeleteAsync(studentEnrollment);
            if (teacherEnrollment != null)
                await _teacherEnrollmentRepository.DeleteAsync(teacherEnrollment);

            response.Success = true;
            response.Message = "You have left the university";

            return response;
        }
    }
}
