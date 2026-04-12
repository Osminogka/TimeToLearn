using AutoMapper;
using Core.DAL.Dtos;
using Core.DAL.Models;

namespace Core.API.Infrastructure
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Source > Destination

            CreateMap<University, ReadUniversityDto>()
                .ForMember(dest => dest.DirectorUsername,
                    opt => opt.MapFrom(src => src.Director != null ? src.Director.Username : string.Empty));
            CreateMap<ReadUniversityDto, University>();

            CreateMap<CreateUniversityDto, University>();

            CreateMap<BaseUserPublishDto, BaseUser>();

            CreateMap<BaseUser, ReadBaseUserDto>()
                .ForMember(dest => dest.IsTeacher, opt => opt.MapFrom(src => src.TeacherId != null));
        }
    }
}
