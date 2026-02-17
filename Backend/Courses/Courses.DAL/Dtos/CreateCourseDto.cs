namespace Courses.DAL.Dtos
{
    public class CreateCourseDto
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string UniversityName { get; set; } = string.Empty;
    }
}
