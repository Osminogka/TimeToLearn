﻿using Authentication.DAL.Dtos;
using Authentication.DAL.Models;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace Authentication.API.Infrastructure
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Source -> Target
            CreateMap<AppUser, BaseUserPublishDto>()
                .ForMember(dest => dest.OriginalId, opt => opt.MapFrom(src => src.Id));   
        }
    }
}
