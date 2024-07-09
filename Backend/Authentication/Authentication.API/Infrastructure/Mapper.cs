using Authentication.DAL.Models;
using Authentication.DL.Models;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace Authentication.API.Infrastructure
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserModel, AppUser>();
            CreateMap<AppUser, UserModel>();

            CreateMap<UserManager<UserModel>, UserManager<AppUser>>();
            CreateMap<UserManager<AppUser>, UserManager<UserModel>>();
        }
    }
}
