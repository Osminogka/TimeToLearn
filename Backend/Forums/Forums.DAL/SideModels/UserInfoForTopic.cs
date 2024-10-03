namespace Forums.DAL.SideModels
{
    public class UserInfoForTopic
    {
        public long UserId { get; set; }

        public long UniversityId { get; set; }

        public bool IsAllowed { get; set; }
    }
}
