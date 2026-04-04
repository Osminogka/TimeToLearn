using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Core.DAL.Dtos;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;

namespace Core.DL.Services
{
    public class UniversityService : IUniversityService
    {
        private readonly IMapper _mapper;
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<BaseUser> _userRepository;
        private readonly IBaseRepository<StudentEnrollment> _studentEnrollmentRepository;
        private readonly IBaseRepository<TeacherEnrollment> _teacherEnrollmentRepository;

        public UniversityService(
            IMapper mapper,
            IBaseRepository<University> universityRepository,
            IBaseRepository<BaseUser> userRepository,
            IBaseRepository<StudentEnrollment> studentEnrollmentRepository,
            IBaseRepository<TeacherEnrollment> teacherEnrollmentRepository)
        {
            _mapper = mapper;
            _universityRepository = universityRepository;
            _userRepository = userRepository;
            _studentEnrollmentRepository = studentEnrollmentRepository;
            _teacherEnrollmentRepository = teacherEnrollmentRepository;
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
            if (director == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var university = _mapper.Map<University>(model);
            university.DirectorId = director.Id;
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
            if (university == null)
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
                .FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var isDirector = await _universityRepository.Where(u => u.Name == universityName && u.DirectorId == user.Id).AnyAsync();
            var hasStudentEnrollment = await _studentEnrollmentRepository.Where(e => e.BaseUserId == user.Id && e.University.Name == universityName).AnyAsync();
            var hasTeacherEnrollment = await _teacherEnrollmentRepository.Where(e => e.BaseUserId == user.Id && e.University.Name == universityName).AnyAsync();

            if (!isDirector && !hasStudentEnrollment && !hasTeacherEnrollment)
            {
                response.Message = "You are not a member of this university";
                return response;
            }

            var enrollments = await _studentEnrollmentRepository.Where(e => e.University.Name == universityName)
                .Include(e => e.BaseUser)
                .ToListAsync();

            response.Enum = enrollments.Select(e => e.BaseUser.Username);
            response.Success = true;
            response.Message = "Got student list";
            return response;
        }

        public async Task<ResponseGetEnum<string>> GetTeachersAsync(string universityName, string userEmail)
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();

            var user = await _userRepository.Where(obj => obj.Email == userEmail)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var isDirector = await _universityRepository.Where(u => u.Name == universityName && u.DirectorId == user.Id).AnyAsync();
            var hasStudentEnrollment = await _studentEnrollmentRepository.Where(e => e.BaseUserId == user.Id && e.University.Name == universityName).AnyAsync();
            var hasTeacherEnrollment = await _teacherEnrollmentRepository.Where(e => e.BaseUserId == user.Id && e.University.Name == universityName).AnyAsync();

            if (!isDirector && !hasStudentEnrollment && !hasTeacherEnrollment)
            {
                response.Message = "You are not a member of this university";
                return response;
            }

            var enrollments = await _teacherEnrollmentRepository.Where(e => e.University.Name == universityName)
                .Include(e => e.BaseUser)
                .ToListAsync();

            response.Enum = enrollments.Select(e => e.BaseUser.Username);
            response.Success = true;
            response.Message = "Got teacher list";
            return response;
        }
    }
}
