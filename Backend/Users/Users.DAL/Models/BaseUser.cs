using System.ComponentModel.DataAnnotations;

namespace Users.DAL.Models
{
    public class BaseUser : BaseEntity
    {
        [Required]
        public Guid OriginalId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public bool IsTeacher { get; set; } = false;

        public long? TeacherId { get; set; }

        public long? StudentId { get; set; }

        public long? UniversityId { get; set; }

        public University? UniversityMember { get; set; }

        public University? UniversityDirector { get; set; }

        public Teacher? Teacher { get; set; }

        public Student? Student { get; set; }

        public ICollection<EntryRequest> EntryRequests { get; set; }
    }
}
