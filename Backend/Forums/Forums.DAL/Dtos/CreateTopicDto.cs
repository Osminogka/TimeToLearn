namespace Forums.DAL.Dtos
{
    public class CreateTopicDto
    {
        public string TopicTitle { get; set; } = string.Empty;

        public string TopicContent { get; set; } = string.Empty;

        public string UniversityName { get; set; } = string.Empty;
    }
}
