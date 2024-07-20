using AutoMapper;
using Users.DAL.Dtos;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;

namespace Users.DL.Services
{
    public class BaseUserService : IBaseUserService
    {
        private readonly IBaseRepository<BaseUser> _baseUserRepository;
        private readonly IMapper _mapper;

        public BaseUserService(IBaseRepository<BaseUser> baseUserRepository, IMapper mapper)
        {
            _baseUserRepository = baseUserRepository;
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

        public async Task<ResponseMessage> UpdateUserInfoAsync(UpdateUserInfo userInfo, string email)
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
    }
}
