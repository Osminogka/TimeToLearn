using System.ComponentModel.DataAnnotations;

namespace Forums.DAL.Models
{
    public class BaseEntity
    {
        [Key]
        public long Id { get; set; }
    }
}
