namespace Courses.DAL.Dtos
{
    public class UpdateCourseDto
    {
        public long CourseId { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }
    }
}
