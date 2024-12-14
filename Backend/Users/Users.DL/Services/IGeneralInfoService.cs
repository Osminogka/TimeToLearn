using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.SideModels;

namespace Users.DL.Services
{
    public interface IGeneralInfoService
    {
        Task<ResponseWithValue<RoleUserInfo>> GetUserRoleAsync(string userEmail);
    }
}
