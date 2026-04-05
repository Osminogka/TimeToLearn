namespace Core.DAL.SideModels
{
    public class PagedResponse<T>
    {
        public bool Success { get; set; } = false;

        public string Message { get; set; } = "Invalid Request";

        public IEnumerable<T> Items { get; set; } = new List<T>();

        public int TotalCount { get; set; }
    }
}
