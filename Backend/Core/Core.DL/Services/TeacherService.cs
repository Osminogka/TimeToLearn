using Microsoft.EntityFrameworkCore;
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
        private readonly IBaseRepository<StudentEnrollment> _studentEnrollmentRepository;
        private readonly IBaseRepository<TeacherEnrollment> _teacherEnrollmentRepository;

        public TeacherService(
            IBaseRepository<Teacher> teacherRepository,
            IBaseRepository<BaseUser> baseUserRepository,
            IBaseRepository<University> universityRepository,
            IBaseRepository<EntryRequest> entryRequestRepository,
            IBaseRepository<StudentEnrollment> studentEnrollmentRepository,
            IBaseRepository<TeacherEnrollment> teacherEnrollmentRepository)
        {
            _teacherRepository = teacherRepository;
            _baseUserRepository = baseUserRepository;
            _universityRepository = universityRepository;
            _entryRequestRepository = entryRequestRepository;
            _studentEnrollmentRepository = studentEnrollmentRepository;
            _teacherEnrollmentRepository = teacherEnrollmentRepository;
        }

        public async Task<ResponseMessage> BecomeTeacherAsync(string email)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _baseUserRepository.Where(obj => obj.Email == email && obj.TeacherId == null)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "Such user doesn't exist or is already a teacher";
                return response;
            }

            var context = _baseUserRepository.GetContext();
            var transaction = context.Database.IsRelational()
                ? await context.Database.BeginTransactionAsync()
                : null;

            try
            {
                var studentEnrollments = await _studentEnrollmentRepository.Where(obj => obj.BaseUserId == user.Id).ToListAsync();
                if (studentEnrollments.Count > 0)
                    await _studentEnrollmentRepository.DeleteRangeAsync(studentEnrollments);

                var entryRequests = await _entryRequestRepository.Where(obj => obj.BaseUserId == user.Id).ToListAsync();
                if (entryRequests.Count > 0)
                    await _entryRequestRepository.DeleteRangeAsync(entryRequests);

                Teacher teacher = new Teacher
                {
                    BaseUserId = user.Id
                };

                await _teacherRepository.AddAsync(teacher);
                user.TeacherId = teacher.Id;

                await _baseUserRepository.UpdateAsync(user);

                if (transaction != null)
                    await transaction.CommitAsync();

                response.Success = true;
                response.Message = "You successfully became a teacher";
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

        public async Task<ResponseMessage> VerifyStatusAsync(string email, string degree)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _baseUserRepository.Where(obj =>
                obj.Email == email && obj.TeacherId != null && obj.Teacher.IsVerified == false)
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

            response.Message = "Teachers can join as teachers by invite only";

            return response;
        }
    }
}
