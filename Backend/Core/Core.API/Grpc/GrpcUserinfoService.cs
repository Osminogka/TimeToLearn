using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Core.DAL.Models;
using Core.DL.Repositories;
using UserService;

namespace Core.API.Grpc
{
    public class GrpcUserInfoService : GrpcUsers.GrpcUsersBase
    {
        private readonly IBaseRepository<BaseUser> _baseUserRepository;
        private readonly IBaseRepository<University> _universityRepository;

        public GrpcUserInfoService(IBaseRepository<BaseUser> baseUserRepository, IBaseRepository<University> universityRepository)
        {
            _baseUserRepository = baseUserRepository;
            _universityRepository = universityRepository;
        }

        public override async Task<GrpcTopicInfoModel> GetInfoForTopic(GetInfoRequest request, ServerCallContext context)
        {
            GrpcTopicInfoModel response = new GrpcTopicInfoModel();
            response.IsAllowed = false;

            BaseUser? user = await _baseUserRepository.Where(obj => obj.Email == request.Useremail)
                .FirstOrDefaultAsync();
            if (user == null)
                return response;

            University? university = await _universityRepository.Where(obj => obj.Name == request.UniversityName)
                .Include(obj => obj.StudentEnrollments)
                .Include(obj => obj.TeacherEnrollments)
                .FirstOrDefaultAsync();

            if (university == null)
                return response;

            bool isDirector = university.DirectorId == user.Id;
            bool hasStudentEnrollment = university.StudentEnrollments.Any(e => e.BaseUserId == user.Id);
            bool hasTeacherEnrollment = university.TeacherEnrollments.Any(e => e.BaseUserId == user.Id);

            if (!isDirector && !hasStudentEnrollment && !hasTeacherEnrollment)
                return response;

            response.UserId = user.Id;
            response.UniversityId = university.Id;
            response.IsAllowed = true;
            response.IsTeacher = hasTeacherEnrollment;

            return response;
        }

        public override async Task<UniversityName> GetUniversityName(UniversityId request, ServerCallContext context)
        {
            UniversityName response = new UniversityName();

            University? university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Id == request.UniversityId_);
            if (university == null)
                return response;

            response.UniversityName_ = university.Name;

            return response;
        }

        public override async Task<UserName> GetUserName(UserId request, ServerCallContext context)
        {
            UserName response = new UserName();

            BaseUser? user = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Id == request.UserId_);
            if (user == null)
                return response;

            response.Username = user.Username;

            return response;
        }
    }
}
