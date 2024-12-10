namespace Forums.DAL.Dtos
{
    public class ReadCommentDto
    {
        public string CreatorName { get; set; } = string.Empty;

        public string CommentContent { get; set; } = string.Empty;

        public long LikesOverall { get; set; } = 0;

        public long DislikesOverall { get; set; } = 0;

        public DateTime CreatedAt { get; set; }
    }
}
