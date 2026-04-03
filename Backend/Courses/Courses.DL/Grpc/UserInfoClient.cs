using AutoMapper;
using Courses.DAL.SideModels;
using UserService;

namespace Courses.DL.Grpc
{
    public class UserInfoClient : IUserInfoClient
    {
        private readonly IMapper _mapper;
        private readonly GrpcUsers.GrpcUsersClient _client;

        public UserInfoClient(GrpcUsers.GrpcUsersClient client, IMapper mapper)
        {
            _mapper = mapper;
            _client = client;
        }

        public async Task<UserInfoForCourse?> GetUserInfoForCourse(string universityName, string userEmail)
        {
            var request = new GetInfoRequest()
            {
                UniversityName = universityName,
                Useremail = userEmail
            };

            try
            {
                var reply = await _client.GetInfoForTopicAsync(request);
                var userInfo = _mapper.Map<UserInfoForCourse>(reply);
                return userInfo;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"--> Couldn't call GRPC Server: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetUniversityName(long universityId)
        {
            try
            {
                var reply = await _client.GetUniversityNameAsync(_mapper.Map<UniversityId>(universityId));
                return reply.UniversityName_;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"--> Couldn't call GRPC Server: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetUserName(long userId)
        {
            try
            {
                var reply = await _client.GetUserNameAsync(_mapper.Map<UserId>(userId));
                return reply.Username;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Couldn't call GRPC Server: {ex.Message}");
                return null;
            }
        }
    }
}
