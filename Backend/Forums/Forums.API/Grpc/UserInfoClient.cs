using AutoMapper;
using Forums.DAL.SideModels;
using Grpc.Net.Client;
using UserService;

namespace Forums.API.Grpc
{
    public class UserInfoClient : IUserInfoClient
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public UserInfoClient(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<UserInfoForTopic?> GetUserInfoForTopic(string universityName, string userEmail)
        {
            var channel = GrpcChannel.ForAddress(_configuration["GrpcUsersApi"]);
            var client = new GrpcUsers.GrpcUsersClient(channel);
            var request = new GetInfoRequest()
            {
                UniversityName = universityName,
                Useremail = userEmail
            };

            try
            {
                var reply = await client.GetInfoForTopicAsync(request);
                return _mapper.Map<UserInfoForTopic>(reply);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"--> Couldn't call GRPC Server: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetUniversityName(long universityId)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetUserName(long userId)
        {
            throw new NotImplementedException();
        }
    }
}
