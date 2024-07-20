using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.Dtos;
using Users.DAL.Models;
using Users.DAL.SideModels;

namespace Users.DL.Services
{
    public interface IUniversityService
    {
        Task<ResponseGetEnum<string>> GetAllAsync();
        Task<ResponseWithValue<ReadUniversityDto>> GetAsync(string name);
        Task<ResponseWithValue<ReadUniversityDto>> CreateAsync(CreateUniversityDto model, string email);
        Task<ResponseGetEnum<string>> GetStudentsAsync(string universityName, string userEmail);
        Task<ResponseGetEnum<string>> GetTeachersAsync(string universityName, string userEmail);
    }
}
