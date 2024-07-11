using AutoMapper;
using Users.DAL.Dtos;
using Users.DAL.Models;
using Users.DL.Repositories;

namespace Users.DL.Services
{
    public class UniversityService : IUniversityService
    {
        private readonly IMapper _mapper;
        private readonly IBaseRepository<University> _universityRepository;

        public UniversityService(IMapper mapper, IBaseRepository<University> universityRepository)
        {
            _mapper = mapper;
            _universityRepository = universityRepository;
        }

        public async Task<ReadUniversityDto?> CreateAsync(CreateUniversityDto model)
        {
            var university = _mapper.Map<University>(model);
            if (await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == model.Name) != null)
                return null;
            await _universityRepository.AddAsync(university);
            return _mapper.Map<ReadUniversityDto>(university);
        }

        public async Task<ReadUniversityDto?> GetUniverAsync(string name)
        {
            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == name);
            if(university != null)
                return _mapper.Map<ReadUniversityDto>(university);
            return null;
        }
    }
}
