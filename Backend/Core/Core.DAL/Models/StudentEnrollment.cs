namespace Core.DAL.Models
{
    public class StudentEnrollment : BaseEntity
    {
        public long BaseUserId { get; set; }

        public long UniversityId { get; set; }

        public BaseUser BaseUser { get; set; }

        public University University { get; set; }
    }
}
