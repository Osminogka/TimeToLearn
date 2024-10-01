namespace Forums.DAL.Dtos
{
    public class CreateTopicDto
    {
        public long TopicCreatorId { get; set; }

        public string TopicTitle { get; set; } = string.Empty;
    }
}
