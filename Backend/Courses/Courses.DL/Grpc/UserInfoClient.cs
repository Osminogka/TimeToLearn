using AutoMapper;
using Courses.DAL.SideModels;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using UserService;

namespace Courses.DL.Grpc
{
    public class UserInfoClient : IUserInfoClient
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly GrpcChannel _channel;
        private readonly GrpcUsers.GrpcUsersClient _client;

        public UserInfoClient(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _mapper = mapper;
            _channel = GrpcChannel.ForAddress(_configuration["GrpcUsersApi"]);
            _client = new GrpcUsers.GrpcUsersClient(_channel);
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
