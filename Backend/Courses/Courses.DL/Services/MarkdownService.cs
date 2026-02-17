using Markdig;

namespace Courses.DL.Services
{
    public class MarkdownService : IMarkdownService
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownService()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .UseSoftlineBreakAsHardlineBreak()
                .Build();
        }

        public string ConvertToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            return Markdown.ToHtml(markdown, _pipeline);
        }
    }
}
