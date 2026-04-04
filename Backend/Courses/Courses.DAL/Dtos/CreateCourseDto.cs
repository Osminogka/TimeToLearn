using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class CreateCourseDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string UniversityName { get; set; } = string.Empty;
    }
}
