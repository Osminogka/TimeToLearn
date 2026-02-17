using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Users.DAL.Dtos;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;

namespace Users.DL.Services
{
    public class UniversityService : IUniversityService
    {
        private readonly IMapper _mapper;
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<BaseUser> _userRepository;

        public UniversityService(IMapper mapper, IBaseRepository<University> universityRepository, IBaseRepository<BaseUser> userRepository)
        {
            _mapper = mapper;
            _universityRepository = universityRepository;
            _userRepository = userRepository;
        }

        public async Task<ResponseGetEnum<string>> GetAllAsync()
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();
            var universities = await _universityRepository.GetAllAsync();
            response.Success = true;
            response.Message = "Got all universities";
            response.Enum = universities.Select(obj => obj.Name).ToList();
            return response;
        }

        public async Task<ResponseWithValue<ReadUniversityDto>> CreateAsync(CreateUniversityDto model, string email)
        {
            ResponseWithValue<ReadUniversityDto> response = new ResponseWithValue<ReadUniversityDto>();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                response.Message = "University name cannot be empty";
                return response;
            }

            var doesUniversityExist = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == model.Name);
            if (doesUniversityExist != null)
            {
                response.Message = "Such university already exists";
                return response;
            }

            var director = await _userRepository.SingleOrDefaultAsync(obj => obj.Email == email);
            if(director == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var isAlreadyDirector = await _universityRepository.SingleOrDefaultAsync(obj => obj.DirectorId == director.Id);
            if (isAlreadyDirector != null)
            {
                response.Message = "You are already a director of another university";
                return response;
            }

            var university = _mapper.Map<University>(model);
            university.DirectorId = director.Id;
            university.Members = new List<BaseUser> { director };
            await _universityRepository.AddAsync(university);

            response.Value = _mapper.Map<ReadUniversityDto>(university);
            response.Success = true;
            response.Message = "University created";
            return response;
        }

        public async Task<ResponseWithValue<ReadUniversityDto>> GetAsync(string name)
        {
            ResponseWithValue<ReadUniversityDto> response = new ResponseWithValue<ReadUniversityDto>();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == name);
            if(university == null)
            {
                response.Message = "Such university doesn't exist";
                return response;
            }

            response.Value = _mapper.Map<ReadUniversityDto>(university);
            response.Success = true;
            response.Message = "Got university";
            return response;
        }

        public async Task<ResponseGetEnum<string>> GetStudentsAsync(string universityName, string userEmail)
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();

            var user = await _userRepository.Where(obj => obj.Email == userEmail)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            if (!user.Universities.Any(u => u.Name == universityName))
            {
                response.Message = "You are not a member of this university";
                return response;
            }

            var university = await _universityRepository.Where(obj => obj.Name == universityName)
                    .Include(obj => obj.Members.Where(obj => obj.IsTeacher == false))
                    .FirstOrDefaultAsync();

            if (university == null)
            {
                response.Message = "Such university doesn't exist or you are not a member";
                return response;
            }

            response.Enum = university.Members.Select(obj => obj.Username);
            response.Success = true;
            response.Message = "Got student list";
            return response;
        }

        public async Task<ResponseGetEnum<string>> GetTeachersAsync(string universityName, string userEmail)
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();

            var user = await _userRepository.Where(obj => obj.Email == userEmail)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();

            if(user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            if (!user.Universities.Any(u => u.Name == universityName))
            {
                response.Message = "You are not a member of this university";
                return response;
            }

            var university = await _universityRepository.Where(obj => obj.Name == universityName)
                    .Include(obj => obj.Members.Where(obj => obj.IsTeacher == true))
                    .FirstOrDefaultAsync();
            
            if(university == null)
            {
                response.Message = "Such university doesn't exist or you are not a member";
                return response;
            }

            response.Enum = university.Members.Select(obj => obj.Username);
            response.Success = true;
            response.Message = "Got teacher list";
            return response;
        }
    }
}
