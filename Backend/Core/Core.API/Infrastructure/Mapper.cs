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

            CreateMap<University, ReadUniversityDto>();
            CreateMap<ReadUniversityDto, University>();

            CreateMap<CreateUniversityDto, University>();

            CreateMap<BaseUserPublishDto, BaseUser>();

            CreateMap<BaseUser, ReadBaseUserDto>();
        }
    }
}
