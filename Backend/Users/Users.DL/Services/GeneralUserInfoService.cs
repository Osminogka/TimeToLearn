using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;

namespace Users.DL.Services
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

            response.Value.isStudent = baseUser.StudentId != null;
            response.Value.isTeacher = baseUser.IsTeacher;

            response.Success = true;
            response.Message = "You got user role info";

            return response;
        }
    }
}
