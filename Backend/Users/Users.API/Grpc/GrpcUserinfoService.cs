using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Users.DAL.Models;
using Users.DL.Repositories;
using UserService;

namespace Users.API.Grpc
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

            BaseUser? user = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Email == request.Useremail);
            if (user == null)
                return response;

            University? university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == request.UniversityName && obj.Members.SingleOrDefault(member => member.Id == user.Id) != null);
            if (university == null)
                return response;

            response.UserId = user.Id;
            response.UniversityId = university.Id;
            response.IsAllowed = true;

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
