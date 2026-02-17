using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Models
{
    public class BaseEntity
    {
        [Key]
        public long Id { get; set; }
    }
}
