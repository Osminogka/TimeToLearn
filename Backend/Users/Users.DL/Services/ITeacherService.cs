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
        Task<ResponseMessage> BecomeTeacherAsync(string email);
        Task<ResponseMessage> VerifyStatusAsync(string email, string degree);
        Task<ResponseMessage> SendRequestToBecomeTeacherOfUniversity(string universityName, string mainUserEmail);
    }
}
