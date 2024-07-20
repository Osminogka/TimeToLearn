using AutoMapper;
using Users.DAL.Dtos;
using Users.DAL.Models;

namespace Users.API.Infrastructure
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
