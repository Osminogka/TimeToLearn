using AutoMapper;
using UserService;
using Courses.DAL.SideModels;

namespace Courses.API.Infrastructure
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<GrpcTopicInfoModel, UserInfoForCourse>()
                .ForMember(dest => dest.UniversityId, opt => opt.MapFrom(src => src.UniversityId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.IsAllowed, opt => opt.MapFrom(src => src.IsAllowed))
                .ForMember(dest => dest.IsTeacher, opt => opt.MapFrom(src => src.IsTeacher));
            CreateMap<UniversityId, long>().ConvertUsing(src => src.UniversityId_);

            CreateMap<long, UniversityId>().ConvertUsing(src => new UniversityId { UniversityId_ = src });
            CreateMap<UserId, long>().ConvertUsing(src => src.UserId_);
            CreateMap<long, UserId>().ConvertUsing(src => new UserId { UserId_ = src });
        }
    }
}
