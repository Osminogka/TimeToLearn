using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class UpdateLessonDto
    {
        [Required]
        public long LessonId { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        [Url]
        [MaxLength(500)]
        public string? VideoLink { get; set; }

        [Url]
        [MaxLength(500)]
        public string? MaterialLink { get; set; }

        public List<LessonResourceDto>? Resources { get; set; }

        public int? OrderNumber { get; set; }
    }
}

