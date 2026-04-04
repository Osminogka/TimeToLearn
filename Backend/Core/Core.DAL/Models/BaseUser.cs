using System.ComponentModel.DataAnnotations;

namespace Core.DAL.Models
{
    public class BaseUser : BaseEntity
    {
        [Required]
        public Guid OriginalId { get; set; }

        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PhoneNumber { get; set; } = string.Empty;

        public Address Address { get; set; }

        public long? TeacherId { get; set; }

        public ICollection<University> DirectingUniversities { get; set; }

        public Teacher? Teacher { get; set; }

        public ICollection<StudentEnrollment> StudentEnrollments { get; set; }

        public ICollection<TeacherEnrollment> TeacherEnrollments { get; set; }

        public ICollection<EntryRequest> EntryRequests { get; set; }
    }
}
