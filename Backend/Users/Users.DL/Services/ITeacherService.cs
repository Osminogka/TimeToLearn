using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.SideModels;

namespace Users.DL.Services
{
    public interface ITeacherService
    {
        Task<ResponseMessage> InviteTeacherToUniversity(string universityName, string username, string mainUserEmail);
        Task<ResponseMessage> SendRequestToBecomeTeacherOfUniversity(string universityName, string mainUserEmail);
    }
}
