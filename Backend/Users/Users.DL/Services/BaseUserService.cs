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
        private readonly IMapper _mapper;

        public BaseUserService(IBaseRepository<BaseUser> baseUserRepository, IBaseRepository<EntryRequest> entryRequestRepository, IBaseRepository<University> universityRepository, IMapper mapper)
        {
            _baseUserRepository = baseUserRepository;
            _entryRequestRepository = entryRequestRepository;
            _universityRepository = universityRepository;
            _mapper = mapper;
        }

        public async Task<ResponseGetEnum<string>> GetUsersAsync()
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();

            var universities = await _baseUserRepository.GetAllAsync();

            response.Success = true;
            response.Message = "Got all users";
            response.Enum = universities.Select(obj => obj.Username).ToList();

            return response;
        }

        public async Task<ResponseWithValue<ReadBaseUserDto>> GetBaseUserAsync(string username)
        {
            ResponseWithValue<ReadBaseUserDto> response = new ResponseWithValue<ReadBaseUserDto>();

            var user = await _baseUserRepository.SingleOrDefaultAsync(obj =>  obj.Username == username);
            if(user == null)
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
            if(user == null)
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
                .ThenInclude(obj => obj.University).FirstOrDefaultAsync();

            if(user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            response.Enum = user.EntryRequests
                .Where(er => er.SentByUniversity && !user.Universities.Any(u => u.Id == er.UniversityId))
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
            
            if(doesRequestExist == null)
            {
                response.Message = "Such invite doesn't exist";
                return response;
            }
            await _entryRequestRepository.DeleteAsync(doesRequestExist);
            
            var user = await _baseUserRepository.Where(obj => obj.Email == email)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();
            if(user == null)
            {
                response.Message = "User not found";
                return response;
            }

            if(user.Universities.Any(u => u.Id == doesRequestExist.UniversityId))
            {
                response.Message = "User already belongs to this university";
                return response;
            }

            var checkForAnotherRequest = await _entryRequestRepository.Where(obj => obj.BaseUserId == user.Id && obj.University.Name == universityName && obj.SentByUniversity == false)
                .Include(obj => obj.University)
                .FirstOrDefaultAsync();
            if(checkForAnotherRequest != null)
                await _entryRequestRepository.DeleteAsync(checkForAnotherRequest);

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Id == doesRequestExist.UniversityId);
            if (university != null)
            {
                university.Members ??= new List<BaseUser>();
                university.Members.Add(user);
                await _universityRepository.UpdateAsync(university);
            }

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
                .Include(obj => obj.Universities)
                .Include(obj => obj.UniversityDirector)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                response.Message = "User not found";
                return response;
            }

            var university = user.Universities.FirstOrDefault(u => u.Name == universityName);
            if (university == null)
            {
                response.Message = "You are not a member of this university";
                return response;
            }

            if (user.UniversityDirector != null && user.UniversityDirector.Id == university.Id)
            {
                response.Message = "You cannot leave a university you are directing. Transfer directorship first";
                return response;
            }

            user.Universities.Remove(university);
            await _baseUserRepository.UpdateAsync(user);

            response.Success = true;
            response.Message = "You have left the university";

            return response;
        }
    }
}
