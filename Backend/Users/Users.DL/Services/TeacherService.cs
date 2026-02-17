using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;

namespace Core.DL.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly IBaseRepository<Teacher> _teacherRepository;
        private readonly IBaseRepository<BaseUser> _baseUserRepository;
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<EntryRequest> _entryRequestRepository;

        public TeacherService(IBaseRepository<Teacher> teacherRepository, IBaseRepository<BaseUser> baseUserRepository, IBaseRepository<University> universityRepository, IBaseRepository<EntryRequest> entryRequestRepository)
        {
            _teacherRepository = teacherRepository;
            _baseUserRepository = baseUserRepository;
            _universityRepository = universityRepository;
            _entryRequestRepository = entryRequestRepository;
        }

        public async Task<ResponseMessage> BecomeTeacherAsync(string email)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _baseUserRepository.SingleOrDefaultAsync(obj => obj.Email == email && obj.IsTeacher == false);
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            Teacher teacher = new Teacher
            {
                BaseUserId = user.Id
            };

            await _teacherRepository.AddAsync(teacher);
            user.TeacherId = teacher.Id;
            user.IsTeacher = true;
            
            if (user.StudentId != null)
            {
                user.StudentId = null;
            }

            await _baseUserRepository.UpdateAsync(user);
            response.Success = true;
            response.Message = "You successfully became a teacher";

            return response;
        }

        public async Task<ResponseMessage> VerifyStatusAsync(string email, string degree)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _baseUserRepository.Where(obj =>
                obj.Email == email && obj.IsTeacher == true && obj.Teacher.IsVerified == false)
                .Include(obj => obj.Teacher)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "Such user doesn't exist or is already verified";
                return response;
            }

            var teacher = await _teacherRepository.SingleOrDefaultAsync(obj => obj.BaseUserId == user.Id);
            if (teacher == null)
            {
                response.Message = "Teacher record not found";
                return response;
            }

            teacher.Degree = degree;
            teacher.IsVerified = true;

            await _teacherRepository.UpdateAsync(teacher);
            response.Success = true;
            response.Message = "You successfully verified";

            return response;
        }
        
        public async Task<ResponseMessage> SendRequestToBecomeTeacherOfUniversity(string universityName, string teacherEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.Where(obj => obj.Name == universityName)
                .Include(obj => obj.Members)
                .FirstOrDefaultAsync();
            if (university == null)
            {
                response.Message = "Such university doesn't exist";
                return response;
            }

            var teacher = await _baseUserRepository.Where(obj => obj.Email == teacherEmail && obj.IsTeacher == true && obj.Teacher.IsVerified == true)
                .Include(obj => obj.Teacher)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();
            if (teacher == null)
            {
                response.Message = "Such user doesn't exist or is not a verified teacher";
                return response;
            }

            if (teacher.Universities.Any(u => u.Id == university.Id))
            {
                response.Message = "You already belong to this university";
                return response;
            }
            
            if (university.Members.Any(obj => obj.Id == teacher.Id))
            {
                response.Message = "You are already a teacher of this university";
                return response;
            }

            var doesEntryRequestExist = await _entryRequestRepository.SingleOrDefaultAsync(
                er =>
                    er.BaseUserId == teacher.Id &&
                    er.UniversityId == university.Id &&
                    er.SentByUniversity == false);

            if (doesEntryRequestExist != null)
            {
                response.Message = "You already sent entry request to this university";
                return response;
            }

            EntryRequest entryRequest = new EntryRequest
            {
                BaseUserId = teacher.Id,
                UniversityId = university.Id,
                SentByUniversity = false
            };

            await _entryRequestRepository.AddAsync(entryRequest);

            response.Success = true;
            response.Message = "Request is sent";

            return response;
        }
    }
}
