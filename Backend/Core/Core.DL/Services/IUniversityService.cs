using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.DAL.Dtos;
using Core.DAL.Models;
using Core.DAL.SideModels;

namespace Core.DL.Services
{
    public interface IUniversityService
    {
        Task<PagedResponse<ReadUniversityDto>> GetPagedAsync(int page, int pageSize);
        Task<PagedResponse<ReadUniversityDto>> GetMyUniversitiesAsync(string email, int page, int pageSize);
        Task<ResponseWithValue<ReadUniversityDto>> GetAsync(string name);
        Task<ResponseWithValue<ReadUniversityDto>> CreateAsync(CreateUniversityDto model, string email);
        Task<PagedResponse<string>> GetStudentsAsync(string universityName, string userEmail, int page, int pageSize);
        Task<PagedResponse<string>> GetTeachersAsync(string universityName, string userEmail, int page, int pageSize);
    }
}
