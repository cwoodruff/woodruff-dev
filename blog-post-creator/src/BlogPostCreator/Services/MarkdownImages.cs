using System.Text.RegularExpressions;

namespace BlogPostCreator.Services;

/// <summary>Port of localImageRefs() in site/scripts/lib/import-utils.mjs.</summary>
public static partial class MarkdownImages
{
    public static IReadOnlyList<string> LocalRefs(string body)
    {
        var refs = new List<string>();
        foreach (var re in new[] { MdImage(), HtmlImage() })
        {
            foreach (Match m in re.Matches(body))
            {
                var p = m.Groups[1].Value;
                if (Remote().IsMatch(p) || p.StartsWith('/') || p.StartsWith("data:")) continue;
                if (!refs.Contains(p)) refs.Add(p);
            }
        }
        return refs;
    }

    [GeneratedRegex(@"!\[[^\]]*\]\(\s*<?([^)\s>]+)>?(?:\s+""[^""]*"")?\s*\)")]
    private static partial Regex MdImage();

    [GeneratedRegex(@"<img[^>]+src=[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlImage();

    [GeneratedRegex(@"^(https?:)?//")]
    private static partial Regex Remote();
}
