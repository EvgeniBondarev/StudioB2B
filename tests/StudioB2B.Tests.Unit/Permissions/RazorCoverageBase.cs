using System.Text.RegularExpressions;

namespace StudioB2B.Tests.Unit.Permissions;

/// <summary>
/// Base helpers for scanning .razor source files for permission-related patterns.
/// </summary>
public abstract class RazorCoverageBase
{
    protected static readonly string RazorRoot =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "StudioB2B.Web", "Components");

    /// <summary>Returns all text content from all .razor and .cs files under the Web project.</summary>
    protected static string GetAllSourceText()
    {
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "StudioB2B.Web"));

        if (!Directory.Exists(webRoot))
            throw new DirectoryNotFoundException($"Web root not found: {webRoot}");

        var files = Directory.GetFiles(webRoot, "*.razor", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(webRoot, "*.cs", SearchOption.AllDirectories));

        return string.Join("\n", files.Select(File.ReadAllText));
    }

    protected static bool ContainsPattern(string text, string pattern)
        => Regex.IsMatch(text, Regex.Escape(pattern));
}
