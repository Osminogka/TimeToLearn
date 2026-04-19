namespace Forums.DAL.Dtos
{
    public class ReadCommentDto
    {
        public long Id { get; set; }

        public long PostId { get; set; }

        public bool IsTopic { get; set; }

        public string CreatorName { get; set; } = string.Empty;

        public string CreatorRole { get; set; } = string.Empty;

        public string CommentContent { get; set; } = string.Empty;

        public long LikesOverall { get; set; } = 0;

        public long DislikesOverall { get; set; } = 0;

        public long RepliesCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; }
    }
}
