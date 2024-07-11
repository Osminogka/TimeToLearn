using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.DAL.Dtos;
using Users.DAL.Models;

namespace Users.DL.Services
{
    public interface IUniversityService
    {
        Task<ReadUniversityDto?> CreateAsync(CreateUniversityDto model);
        Task<ReadUniversityDto?> GetUniverAsync(string name);
    }
}
