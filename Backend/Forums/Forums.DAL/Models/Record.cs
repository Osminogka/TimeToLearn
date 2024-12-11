using System.ComponentModel.DataAnnotations;

namespace Forums.DAL.Models
{
    public class Record : BaseEntity
    {
        [Required]
        public long CreatorId { get; set; }

        [Required]
        public long UniversityId { get; set; }
    }
}
