using Markdig;
using Microsoft.AspNetCore.Components;

namespace StudioB2B.Web.Helpers;

public static class MarkdownHelper
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UseEmphasisExtras()
        .UseTaskLists()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    /// <summary>
    /// Converts a Markdown string to a sanitized <see cref="MarkupString"/> safe for Blazor rendering.
    /// </summary>
    public static MarkupString ToMarkup(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return new MarkupString(string.Empty);

        var html = Markdown.ToHtml(markdown, Pipeline);
        return new MarkupString(html);
    }
}

