using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class AssignCourseGradeDto
    {
        [Required]
        public long StudentId { get; set; }

        [Range(1, 10)]
        public int Mark { get; set; }
    }
}
