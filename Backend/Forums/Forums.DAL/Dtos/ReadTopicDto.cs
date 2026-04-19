namespace Forums.DAL.Dtos
{
    public class ReadTopicDto
    {
        public long Id { get; set; }

        public string TopicTitle { get; set; }

        public string TopicContent { get; set; }

        public string CreatorName { get; set; }

        public string CreatorRole { get; set; }

        public DateTime CreatedAt { get; set; }

        public long Likes { get; set; }

        public long Dislikes { get; set; }
    }
}
