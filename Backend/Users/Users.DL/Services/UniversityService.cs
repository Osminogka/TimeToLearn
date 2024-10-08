﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Users.DAL.Dtos;
using Users.DAL.Models;
using Users.DAL.SideModels;
using Users.DL.Repositories;

namespace Users.DL.Services
{
    public class UniversityService : IUniversityService
    {
        private readonly IMapper _mapper;
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<BaseUser> _userRepository;

        public UniversityService(IMapper mapper, IBaseRepository<University> universityRepository, IBaseRepository<BaseUser> userRepository)
        {
            _mapper = mapper;
            _universityRepository = universityRepository;
            _userRepository = userRepository;
        }

        public async Task<ResponseGetEnum<string>> GetAllAsync()
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();
            var universities = await _universityRepository.GetAllAsync();
            response.Success = true;
            response.Message = "Got all universities";
            response.Enum = universities.Select(obj => obj.Name).ToList();
            return response;
        }

        public async Task<ResponseWithValue<ReadUniversityDto>> CreateAsync(CreateUniversityDto model, string email)
        {
            ResponseWithValue<ReadUniversityDto> response = new ResponseWithValue<ReadUniversityDto>();

            var director = await _userRepository.SingleOrDefaultAsync(obj => obj.Email == email);
            if(director == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }
            var university = _mapper.Map<University>(model);
            university.DirectorId = director.Id;
            var doesUniversityExist = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == model.Name);
            if (doesUniversityExist != null)
            {
                response.Message = "Such university already exist";
                return response;
            }
            await _universityRepository.AddAsync(university);

            director.UniversityId = university.Id;
            await _userRepository.UpdateAsync(director);

            response.Value = _mapper.Map<ReadUniversityDto>(university);
            response.Success = true;
            response.Message = "University created";
            return response;
        }

        public async Task<ResponseWithValue<ReadUniversityDto>> GetAsync(string name)
        {
            ResponseWithValue<ReadUniversityDto> response = new ResponseWithValue<ReadUniversityDto>();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == name);
            if(university == null)
            {
                response.Message = "Such university doesn't exist";
                return response;
            }

            response.Value = _mapper.Map<ReadUniversityDto>(university);
            response.Success = true;
            response.Message = "Got university";
            return response;
        }

        public async Task<ResponseGetEnum<string>> GetStudentsAsync(string universityName, string userEmail)
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();

            var user = await _userRepository.SingleOrDefaultAsync(obj => obj.Email == userEmail && obj.UniversityMember.Name == universityName);

            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var university = await _universityRepository.Where(obj => obj.Name == universityName)
                    .Include(obj => obj.Members.Where(obj => obj.IsTeacher == false))
                    .FirstOrDefaultAsync();

            if (university == null)
            {
                response.Message = "Such university doesn't exist or you are not a member";
                return response;
            }

            response.Enum = university.Members.Select(obj => obj.Username);
            response.Success = true;
            response.Message = "Got student list";
            return response;
        }

        public async Task<ResponseGetEnum<string>> GetTeachersAsync(string universityName, string userEmail)
        {
            ResponseGetEnum<string> response = new ResponseGetEnum<string>();

            var user = await _userRepository.SingleOrDefaultAsync(obj => obj.Email == userEmail && obj.UniversityMember.Name == universityName);

            if(user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var university = await _universityRepository.Where(obj => obj.Name == universityName)
                    .Include(obj => obj.Members.Where(obj => obj.IsTeacher == true))
                    .FirstOrDefaultAsync();
            
            if(university == null)
            {
                response.Message = "Such university doesn't exist or you are not a member";
                return response;
            }

            response.Enum = university.Members.Select(obj => obj.Username);
            response.Success = true;
            response.Message = "Got teacher list";
            return response;
        }
    }
}
