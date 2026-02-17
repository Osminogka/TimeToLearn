namespace Courses.DAL.SideModels
{
    public class ResponseMessage
    {
        public bool Success { get; set; } = false;

        public string Message { get; set; } = "Invalid request";
    }
}
