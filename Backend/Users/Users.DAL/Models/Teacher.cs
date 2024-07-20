using System.ComponentModel.DataAnnotations;

namespace Users.DAL.Models
{
    public class Teacher : BaseEntity
    {
        public string Degree { get; set; } = string.Empty;

        public bool IsVerified { get; set; } = false;

        public long BaseUserId { get; set; }

        public BaseUser BaseUser { get; set; }
    }
}
