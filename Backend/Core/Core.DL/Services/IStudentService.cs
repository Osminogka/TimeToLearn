using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.DAL.SideModels;

namespace Core.DL.Services
{
    public interface IStudentService
    {
        Task<ResponseMessage> BecomeAStudentAsync(string email);
        Task<ResponseMessage> SendRequestToBecomeStudentOfUniversity(string universityName, string mainUserEmail);
        Task<ResponseMessage> EntryUniversityAsync(string universityName, string userEmail);
    }
}
