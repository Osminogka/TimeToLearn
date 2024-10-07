namespace Forums.DAL.Dtos
{
    public class ReadTopicDto
    {
        public string TopicTitle { get; set; }

        public string TopicContent { get; set; }

        public string CreatorName { get; set; }

        public int Likes { get; set; }

        public int Dislikes { get; set; }
    }
}
