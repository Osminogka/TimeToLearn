using Microsoft.EntityFrameworkCore;
using Core.DAL.Models;
using Core.DAL.SideModels;
using Core.DL.Repositories;

namespace Core.DL.Services
{
    public class StudentService : IStudentService
    {
        private readonly IBaseRepository<StudentEnrollment> _studentEnrollmentRepository;
        private readonly IBaseRepository<TeacherEnrollment> _teacherEnrollmentRepository;
        private readonly IBaseRepository<BaseUser> _baseUserRepository;
        private readonly IBaseRepository<University> _universityRepository;
        private readonly IBaseRepository<EntryRequest> _entryRequestRepository;
        private readonly IBaseRepository<Teacher> _teacherRepository;

        public StudentService(
            IBaseRepository<StudentEnrollment> studentEnrollmentRepository,
            IBaseRepository<TeacherEnrollment> teacherEnrollmentRepository,
            IBaseRepository<BaseUser> baseUserRepository,
            IBaseRepository<University> universityRepository,
            IBaseRepository<EntryRequest> entryRequestRepository,
            IBaseRepository<Teacher> teacherRepository)
        {
            _studentEnrollmentRepository = studentEnrollmentRepository;
            _teacherEnrollmentRepository = teacherEnrollmentRepository;
            _baseUserRepository = baseUserRepository;
            _universityRepository = universityRepository;
            _entryRequestRepository = entryRequestRepository;
            _teacherRepository = teacherRepository;
        }

        // Switch from teacher back to student: clears Teacher record and all TeacherEnrollments.
        public async Task<ResponseMessage> BecomeAStudentAsync(string email)
        {
            ResponseMessage response = new ResponseMessage();

            var user = await _baseUserRepository.Where(obj => obj.Email == email && obj.TeacherId != null)
                .FirstOrDefaultAsync();
            if (user == null)
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
                var teacher = await _teacherRepository.SingleOrDefaultAsync(obj => obj.BaseUserId == user.Id);
                if (teacher != null)
                    await _teacherRepository.DeleteAsync(teacher);

                var teacherEnrollments = await _teacherEnrollmentRepository.Where(obj => obj.BaseUserId == user.Id).ToListAsync();
                if (teacherEnrollments.Count > 0)
                    await _teacherEnrollmentRepository.DeleteRangeAsync(teacherEnrollments);

                var entryRequests = await _entryRequestRepository.Where(obj => obj.BaseUserId == user.Id).ToListAsync();
                if (entryRequests.Count > 0)
                    await _entryRequestRepository.DeleteRangeAsync(entryRequests);

                user.TeacherId = null;
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

            var mainUser = await _baseUserRepository.Where(obj => obj.Email == mainUserEmail)
                .FirstOrDefaultAsync();
            if (mainUser == null)
            {
                response.Message = "Invalid user";
                return response;
            }

            var alreadyEnrolled = await _studentEnrollmentRepository.SingleOrDefaultAsync(
                e => e.BaseUserId == mainUser.Id && e.UniversityId == university.Id);
            if (alreadyEnrolled != null)
            {
                response.Message = "You already belong to this university";
                return response;
            }

            var doesEntryRequestExist = await _entryRequestRepository.SingleOrDefaultAsync(
                er => er.BaseUserId == mainUser.Id && er.UniversityId == university.Id && er.SentByUniversity == false);
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

            var user = await _baseUserRepository.Where(obj => obj.Email == userEmail)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                response.Message = "Such user doesn't exist";
                return response;
            }

            var alreadyEnrolled = await _studentEnrollmentRepository.SingleOrDefaultAsync(
                e => e.BaseUserId == user.Id && e.UniversityId == university.Id);
            if (alreadyEnrolled != null)
            {
                response.Message = "You already belong to this university";
                return response;
            }

            var teacherEnrollment = await _teacherEnrollmentRepository.SingleOrDefaultAsync(
                e => e.BaseUserId == user.Id && e.UniversityId == university.Id);
            if (teacherEnrollment != null)
            {
                response.Message = "You already belong to this university";
                return response;
            }

            var entryRequestCheck = await _entryRequestRepository.Where(obj => obj.UniversityId == university.Id && obj.BaseUserId == user.Id).ToListAsync();
            if (!university.IsOpened)
            {
                var studentInvite = entryRequestCheck.FirstOrDefault(obj => obj.SentByUniversity && obj.InviteAsTeacher == false);
                if (studentInvite == null)
                {
                    var teacherInvite = entryRequestCheck.FirstOrDefault(obj => obj.SentByUniversity && obj.InviteAsTeacher);
                    response.Message = teacherInvite != null
                        ? "This invite allows joining as teacher only"
                        : "You are not allowed to entry";
                    return response;
                }
            }

            if (entryRequestCheck.Count > 0)
                await _entryRequestRepository.DeleteRangeAsync(entryRequestCheck);

            await _studentEnrollmentRepository.AddAsync(new StudentEnrollment
            {
                BaseUserId = user.Id,
                UniversityId = university.Id
            });

            response.Success = true;
            response.Message = "You entered this university";

            return response;
        }
    }
}
