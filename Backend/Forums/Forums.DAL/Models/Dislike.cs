using System.ComponentModel.DataAnnotations;

namespace Forums.DAL.Models
{
    public class Dislike : BaseEntity
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public bool IsTopic { get; set; }

        [Required]
        public long PostId { get; set; }

        public Topic? Topic { get; set; }

        public Comment? Comment { get; set; }
    }
}
