using System.ComponentModel.DataAnnotations;

namespace Users.DAL.Models
{
    public class Teacher : BaseEntity
    {
        [Required]
        public string Degree { get; set; }

        public long BaseUserId { get; set; }

        public BaseUser BaseUser { get; set; }
    }
}
