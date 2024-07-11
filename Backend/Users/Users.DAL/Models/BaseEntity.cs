using System.ComponentModel.DataAnnotations;

namespace Users.DAL.Models
{
    public class BaseEntity
    {
        [Key]
        public long Id { get; set; }
    }
}
