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

        public async Task<PagedResponse<ReadUniversityDto>> GetPagedAsync(int page, int pageSize)
        {
            PagedResponse<ReadUniversityDto> response = new PagedResponse<ReadUniversityDto>();

            var totalCount = await _universityRepository.Where(_ => true).CountAsync();
            var universities = await _universityRepository.Where(_ => true)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = universities.Select(u => _mapper.Map<ReadUniversityDto>(u)).ToList();
            response.TotalCount = totalCount;
            response.Success = true;
            response.Message = "Got universities";
            return response;
        }

        public async Task<PagedResponse<ReadUniversityDto>> GetAvailableAsync(string email, int page, int pageSize)
        {
            PagedResponse<ReadUniversityDto> response = new PagedResponse<ReadUniversityDto>();

            var user = await _userRepository.Where(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var studentUnivIds = await _studentEnrollmentRepository
                .Where(e => e.BaseUserId == user.Id)
                .Select(e => e.UniversityId)
                .ToListAsync();

            var teacherUnivIds = await _teacherEnrollmentRepository
                .Where(e => e.BaseUserId == user.Id)
                .Select(e => e.UniversityId)
                .ToListAsync();

            var directorUnivIds = await _universityRepository
                .Where(u => u.DirectorId == user.Id)
                .Select(u => u.Id)
                .ToListAsync();

            var blockedIds = studentUnivIds.Union(teacherUnivIds).Union(directorUnivIds).Distinct().ToList();

            var query = _universityRepository.Where(u => !blockedIds.Contains(u.Id));
            var totalCount = await query.CountAsync();
            var universities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = universities.Select(u => _mapper.Map<ReadUniversityDto>(u)).ToList();
            response.TotalCount = totalCount;
            response.Success = true;
            response.Message = "Got available universities";

            return response;
        }

        public async Task<PagedResponse<ReadUniversityDto>> GetMyUniversitiesAsync(string email, int page, int pageSize)
        {
            PagedResponse<ReadUniversityDto> response = new PagedResponse<ReadUniversityDto>();

            var user = await _userRepository.Where(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var studentUnivIds = await _studentEnrollmentRepository
                .Where(e => e.BaseUserId == user.Id)
                .Select(e => e.UniversityId)
                .ToListAsync();

            var teacherUnivIds = await _teacherEnrollmentRepository
                .Where(e => e.BaseUserId == user.Id)
                .Select(e => e.UniversityId)
                .ToListAsync();

            var directorUnivIds = await _universityRepository
                .Where(u => u.DirectorId == user.Id)
                .Select(u => u.Id)
                .ToListAsync();

            var allIds = studentUnivIds.Union(teacherUnivIds).Union(directorUnivIds).Distinct().ToList();

            var totalCount = allIds.Count;
            var universities = await _universityRepository
                .Where(u => allIds.Contains(u.Id))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = universities.Select(u => _mapper.Map<ReadUniversityDto>(u)).ToList();
            response.TotalCount = totalCount;
            response.Success = true;
            response.Message = "Got user universities";
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

        public async Task<ResponseWithValue<ReadUniversityDto>> GetAsync(string name, string userEmail)
        {
            ResponseWithValue<ReadUniversityDto> response = new ResponseWithValue<ReadUniversityDto>();

            var university = await _universityRepository.Where(obj => obj.Name == name)
                .Include(obj => obj.Director)
                .FirstOrDefaultAsync();
            if (university == null)
            {
                response.Message = "Such university doesn't exist";
                return response;
            }

            var user = await _userRepository.Where(obj => obj.Email == userEmail).FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var isDirector = university.DirectorId == user.Id;
            var hasStudentEnrollment = await _studentEnrollmentRepository
                .Where(e => e.BaseUserId == user.Id && e.UniversityId == university.Id)
                .AnyAsync();
            var hasTeacherEnrollment = await _teacherEnrollmentRepository
                .Where(e => e.BaseUserId == user.Id && e.UniversityId == university.Id)
                .AnyAsync();

            if (!isDirector && !hasStudentEnrollment && !hasTeacherEnrollment)
            {
                response.Message = "You are not a member of this university";
                return response;
            }

            response.Value = _mapper.Map<ReadUniversityDto>(university);
            response.Success = true;
            response.Message = "Got university";
            return response;
        }

        public async Task<PagedResponse<string>> GetStudentsAsync(string universityName, string userEmail, int page, int pageSize)
        {
            PagedResponse<string> response = new PagedResponse<string>();

            var user = await _userRepository.Where(obj => obj.Email == userEmail).FirstOrDefaultAsync();
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

            var query = _studentEnrollmentRepository.Where(e => e.University.Name == universityName);
            var totalCount = await query.CountAsync();
            var enrollments = await query
                .Include(e => e.BaseUser)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = enrollments.Select(e => e.BaseUser.Username).ToList();
            response.TotalCount = totalCount;
            response.Success = true;
            response.Message = "Got student list";
            return response;
        }

        public async Task<PagedResponse<string>> GetTeachersAsync(string universityName, string userEmail, int page, int pageSize)
        {
            PagedResponse<string> response = new PagedResponse<string>();

            var user = await _userRepository.Where(obj => obj.Email == userEmail).FirstOrDefaultAsync();
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

            var query = _teacherEnrollmentRepository.Where(e => e.University.Name == universityName);
            var totalCount = await query.CountAsync();
            var enrollments = await query
                .Include(e => e.BaseUser)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = enrollments.Select(e => e.BaseUser.Username).ToList();
            response.TotalCount = totalCount;
            response.Success = true;
            response.Message = "Got teacher list";
            return response;
        }
    }
}
