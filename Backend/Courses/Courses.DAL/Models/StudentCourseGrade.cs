using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Models
{
    public class StudentCourseGrade : BaseEntity
    {
        [Required]
        public long StudentId { get; set; }

        [Required]
        public long CourseId { get; set; }

        public Course Course { get; set; }

        [Range(1, 10)]
        public int Mark { get; set; }

        [Required]
        public long GivenByTeacherId { get; set; }

        public DateTime GivenAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
