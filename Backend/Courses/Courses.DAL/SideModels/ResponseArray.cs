namespace Courses.DAL.SideModels
{
    public class ResponseArray<T>
    {
        public bool Success { get; set; } = false;

        public string Message { get; set; } = "Invalid request";

        public IEnumerable<T> Values { get; set; } = new List<T>();
    }
}
