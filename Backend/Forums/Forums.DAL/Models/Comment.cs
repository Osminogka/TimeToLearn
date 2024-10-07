using System.ComponentModel.DataAnnotations;

namespace Forums.DAL.Models
{
    public class Comment : BaseEntity
    {
        [Required]
        public long CommentCreatorId { get; set; }

        [Required]
        public long PostId { get; set; }

        [Required]
        public bool IsTopic { get; set; }

        [Required]
        [MaxLength(1000)]
        public string CommentContent { get; set; } = string.Empty;

        public ICollection<Like> Likes { get; set; }

        public ICollection<Dislike> Dislikes { get; set; }
    }
}
