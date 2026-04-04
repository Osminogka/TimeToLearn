using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;

namespace Core.DL.Services
{
    public class GeneralUserInfoService : IGeneralInfoService
    {
        private readonly IBaseRepository<BaseUser> _baseUserRepository;

        public GeneralUserInfoService(IBaseRepository<BaseUser> baseUserRepository)
        {
            _baseUserRepository = baseUserRepository;
        }

        public async Task<ResponseWithValue<RoleUserInfo>> GetUserRoleAsync(string userEmail)
        {
            ResponseWithValue<RoleUserInfo> response = new ResponseWithValue<RoleUserInfo>();
            response.Value = new RoleUserInfo();
            response.Message = "Such user doesn't exist";

            var baseUser = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Email == userEmail);
            if (baseUser == null)
                return response;

            response.Value.isTeacher = baseUser.TeacherId != null;
            response.Value.isStudent = baseUser.TeacherId == null;

            response.Success = true;
            response.Message = "You got user role info";

            return response;
        }
    }
}
