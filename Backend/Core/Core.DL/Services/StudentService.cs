using Microsoft.EntityFrameworkCore;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;

namespace Core.DL.Services
{
    public class StudentService : IStudentService
    {
        private readonly IBaseRepository<Student> _studentRepository;
        private readonly IBaseRepository<BaseUser> _baseUserRepository;
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<EntryRequest> _entryRequestRepository;
        private readonly IBaseRepository<Teacher> _teacherRepository;

        public StudentService(IBaseRepository<Student> studentRepository, IBaseRepository<BaseUser> baseUserRepository, IBaseRepository<University> universityRepository, IBaseRepository<EntryRequest> entryRequestRepository, IBaseRepository<Teacher> teacherRepository)
        {
            _studentRepository = studentRepository;
            _baseUserRepository = baseUserRepository;
            _universityRepository = universityRepository;
            _entryRequestRepository = entryRequestRepository;
            _teacherRepository = teacherRepository;
        }

        public async Task<ResponseMessage> BecomeAStudentAsync(string email)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _baseUserRepository.Where(obj => obj.Email == email && obj.StudentId == null)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();
            if(user == null)
            {
                response.Message = "Such user doesn't exist or is already a student";
                return response;
            }

            var context = _baseUserRepository.GetContext();
            var transaction = context.Database.IsRelational()
                ? await context.Database.BeginTransactionAsync()
                : null;

            try
            {
                if (user.TeacherId != null)
                {
                    var oldTeacher = await _teacherRepository.SingleOrDefaultAsync(obj => obj.BaseUserId == user.Id);
                    if (oldTeacher != null)
                        await _teacherRepository.DeleteAsync(oldTeacher);

                    user.TeacherId = null;
                }

                var entryRequests = await _entryRequestRepository.Where(obj => obj.BaseUserId == user.Id).ToListAsync();
                if (entryRequests.Count > 0)
                    await _entryRequestRepository.DeleteRangeAsync(entryRequests);

                user.Universities?.Clear();

                Student student = new Student
                {
                    BaseUserId = user.Id
                };

                await _studentRepository.AddAsync(student);
                user.StudentId = student.Id;

                await _baseUserRepository.UpdateAsync(user);

                if (transaction != null)
                    await transaction.CommitAsync();

                response.Success = true;
                response.Message = "You successfully became a student";
            }
            catch
            {
                if (transaction != null)
                    await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }

            return response;
        }
        

        public async Task<ResponseMessage> SendRequestToBecomeStudentOfUniversity(string universityName, string mainUserEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == universityName && obj.IsOpened == true);
            
            if (university == null)
            {
                response.Message = "Such university doesn't exist or is not open for requests";
                return response;
            }

            var mainUser = await _baseUserRepository.Where(obj => obj.Email == mainUserEmail && obj.StudentId != null)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();

            if (mainUser == null)
            {
                response.Message = "Invalid user";
                return response;
            }

            if (mainUser.Universities.Any(u => u.Id == university.Id))
            {
                response.Message = "You already belong to this university";
                return response;
            }

            var doesEntryRequestExist = await _entryRequestRepository.SingleOrDefaultAsync(
                er =>
                    er.BaseUserId == mainUser.Id &&
                    er.UniversityId == university.Id &&
                    er.SentByUniversity == false);

            if (doesEntryRequestExist != null)
            {
                response.Message = "You already sent entry request to this university";
                return response;
            }

            EntryRequest entryRequest = new EntryRequest
            {
                BaseUserId = mainUser.Id,
                UniversityId = university.Id,
                SentByUniversity = false
            };

            await _entryRequestRepository.AddAsync(entryRequest);

            response.Success = true;
            response.Message = "Request is sent";

            return response;
        }

        public async Task<ResponseMessage> EntryUniversityAsync(string universityName, string userEmail)
        {
            ResponseMessage response = new ResponseMessage();

            var university = await _universityRepository.SingleOrDefaultAsync(obj => obj.Name == universityName);
            if (university == null)
            {
                response.Message = "Such university doesn't exist";
                return response;
            }
            
            var user = await _baseUserRepository.Where(obj => obj.Email == userEmail && obj.StudentId != null)
                .Include(obj => obj.Universities)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            if (user.Universities.Any(u => u.Id == university.Id))
            {
                response.Message = "You already belong to this university";
                return response;
            }

            var entryRequestCheck = await _entryRequestRepository.Where(obj => obj.UniversityId == university.Id && obj.BaseUserId == user.Id).ToListAsync();
            if (!university.IsOpened && entryRequestCheck.FirstOrDefault(obj => obj.SentByUniversity) == null)
            {
                response.Message = "You are not allowed to entry";
                return response;
            }

            if (entryRequestCheck.Count > 0)
                await _entryRequestRepository.DeleteRangeAsync(entryRequestCheck);

            university.Members ??= new List<BaseUser>();
            university.Members.Add(user);
            await _universityRepository.UpdateAsync(university);

            response.Success = true;
            response.Message = "You entered this university";

            return response;
        }
    }
}
