using System.ComponentModel.DataAnnotations;

namespace Forums.DAL.Models
{
    public class Topic : BaseEntity
    {
        [Required]
        public long UniversityId { get; set; }

        [Required]
        public long TopicCreatorId { get; set; }

        [Required]
        [MaxLength(30)]
        public string TopicTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string TopicContent { get; set; } = string.Empty;

        public ICollection<Like> Likes { get; set; }

        public ICollection<Dislike> Dislikes { get; set; }
    }
}
