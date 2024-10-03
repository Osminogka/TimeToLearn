using AutoMapper;
using UserService;
using Forums.DAL.SideModels;

namespace Forums.API.Infrastructure
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Source > Destination

            CreateMap<GrpcTopicInfoModel, UserInfoForTopic>()
                .ForMember(dest => dest.UniversityId, opt => opt.MapFrom(src => src.UniversityId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.IsAllowed, opt => opt.MapFrom(src => src.IsAllowed));
        }
    }
}
