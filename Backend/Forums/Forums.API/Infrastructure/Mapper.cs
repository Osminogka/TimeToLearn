using AutoMapper;
using UserService;
using Forums.DAL.SideModels;
using Forums.DAL.Models;
using Forums.DAL.Dtos;

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
            CreateMap<UniversityId, long>().ConvertUsing(src => src.UniversityId_);
            CreateMap<long, UniversityId>().ConvertUsing(src => new UniversityId { UniversityId_ = src });
            CreateMap<Topic, ReadTopicDto>()
                .ForMember(dest => dest.TopicContent, opt => opt.MapFrom(src => src.TopicContent))
                .ForMember(dest => dest.TopicTitle, opt => opt.MapFrom(src => src.TopicTitle))
                .ForMember(dest => dest.Likes, opt => opt.MapFrom(src => src.LikesOverall))
                .ForMember(dest => dest.Dislikes, opt => opt.MapFrom(src => src.DislikesOverall));
        }
    }
}
