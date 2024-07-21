using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Users.DAL.Dtos;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;

namespace Users.DL.Services
{
    public class BaseUserService : IBaseUserService
    {
        private readonly IBaseRepository<BaseUser> _baseUserRepository;
        private readonly IBaseRepository<EntryRequest> _entryRequestRepository;
        private readonly IMapper _mapper;

        public BaseUserService(IBaseRepository<BaseUser> baseUserRepository,IBaseRepository<EntryRequest> entryRequestRepository,IMapper mapper)
        {
            _baseUserRepository = baseUserRepository;
            _entryRequestRepository = entryRequestRepository;
            _mapper = mapper;
        }

        public async Task<ResponseGetEnum<string>> GetUsersAsync()
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();

            var universities = await _baseUserRepository.GetAllAsync();

            response.Success = true;
            response.Message = "Got all universities";
            response.Enum = universities.Select(obj => obj.Username).ToList();

            return response;
        }

        public async Task<ResponseWithValue<ReadBaseUserDto>> GetBaseUserAsync(string email)
        {
            ResponseWithValue<ReadBaseUserDto> response = new ResponseWithValue<ReadBaseUserDto>();

            var user = await _baseUserRepository.SingleOrDefaultAsync(obj =>  obj.Email == email);
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

            user.FirstName = userInfo.FirstName ?? "";
            user.LastName = userInfo.LastName ?? "";
            user.PhoneNumber = userInfo.Phone ?? "";
            user.Address = userInfo.Address ?? new Address();

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
                .ThenInclude(obj => obj.University).FirstAsync();

            if(user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            response.Enum = user.EntryRequests.Select(obj => obj.University.Name);
            response.Success = true;
            response.Message = "Got invites of user";

            return response;
        }

        public async Task<ResponseMessage> AcceptInviteAsync(string universityName, string email)
        {
            ResponseMessage response = new ResponseMessage();

            var doesRequestExist = await _entryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == email && obj.University.Name == universityName && obj.SentByUniversity == true);
            
            if(doesRequestExist == null)
            {
                response.Message = "Such invite doesn't exist";
                return response;
            }
            await _entryRequestRepository.DeleteAsync(doesRequestExist);
            
            var user = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Email == email);

            var checkForAnotherRequest = await _entryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUserId == user.Id && obj.University.Name == universityName && obj.SentByUniversity == false);
            if(checkForAnotherRequest != null)
                await _entryRequestRepository.DeleteAsync(checkForAnotherRequest);

            user!.UniversityId = doesRequestExist.UniversityId;

            await _baseUserRepository.UpdateAsync(user);

            response.Success = true;
            response.Message = "Invite accepted";

            return response;
        }

        public async Task<ResponseMessage> RejectInviteAsync(string universityName, string email)
        {
            ResponseMessage response = new ResponseMessage();

            var doesRequestExist = await _entryRequestRepository.SingleOrDefaultAsync(obj => obj.BaseUser.Email == email && obj.University.Name == universityName && obj.SentByUniversity == true);

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
    }
}
