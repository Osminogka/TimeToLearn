using System.ComponentModel.DataAnnotations;

namespace Users.DAL.Models
{
    public class University : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public long DirectorId { get; set; }

        [Required]
        public Address Address { get; set; }

        [Required]
        public bool IsOpened { get; set; }

        public BaseUser Director { get; set; }

        public ICollection<BaseUser> Members { get; set; }

        public ICollection<EntryRequest> EntryRequests { get; set; }
    }
}
