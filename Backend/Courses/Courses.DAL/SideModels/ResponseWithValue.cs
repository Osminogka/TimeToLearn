namespace Courses.DAL.SideModels
{
    public class ResponseWithValue<T>
    {
        public bool Success { get; set; } = false;

        public string Message { get; set; } = "Invalid request";

        public T? Value { get; set; }
    }
}
