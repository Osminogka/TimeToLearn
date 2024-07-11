using AutoMapper;
using Users.DAL.Dtos;

namespace Users.DL.Services
{
    public class UniversityService : IUniversityService
    {
        private readonly IMapper _mapper;

        public UniversityService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public Task<int> CreateAsync(CreateUniversityDto model)
        {
            throw new NotImplementedException();
        }

        public Task<ReadUniversityDto> GetUniverAsync(string name)
        {
            throw new NotImplementedException();
        }
    }
}
