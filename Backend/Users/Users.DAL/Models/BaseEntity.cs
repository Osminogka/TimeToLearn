using System.ComponentModel.DataAnnotations;

namespace Core.DAL.Models
{
    public class BaseEntity
    {
        [Key]
        public long Id { get; set; }
    }
}
