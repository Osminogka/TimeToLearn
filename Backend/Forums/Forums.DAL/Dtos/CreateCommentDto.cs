namespace Forums.DAL.Dtos
{
    public class CreateCommentDto
    {
        public long PostId { get; set; }

        public bool IsTopic { get; set; }

        public string UniversityName { get; set; } = string.Empty;

        public string CommentContent { get; set; } = string.Empty;
    }
}
