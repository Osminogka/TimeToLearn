using System.ComponentModel.DataAnnotations;

namespace Courses.DAL.Dtos
{
    public class ReorderLessonsDto
    {
        [Required]
        public long CourseId { get; set; }

        [Required]
        public List<LessonOrderItemDto> Items { get; set; } = new();
    }

    public class LessonOrderItemDto
    {
        [Required]
        public long LessonId { get; set; }

        [Required]
        public int OrderNumber { get; set; }
    }
}
