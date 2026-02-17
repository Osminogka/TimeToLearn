using Markdig;

namespace Courses.DL.Services
{
    public interface IMarkdownService
    {
        string ConvertToHtml(string markdown);
    }
}
