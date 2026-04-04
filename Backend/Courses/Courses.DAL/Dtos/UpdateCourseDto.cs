using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class UpdateCourseDto
    {
        [Required]
        public long CourseId { get; set; }

        [MaxLength(100)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
