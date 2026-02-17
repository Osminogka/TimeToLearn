namespace Courses.DAL.SideModels
{
    public class UserInfoForCourse
    {
        public long UserId { get; set; }

        public long UniversityId { get; set; }

        public bool IsAllowed { get; set; }

        public bool IsTeacher { get; set; }
    }
}
