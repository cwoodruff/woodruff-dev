using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlogPostCreator.Models;
using YamlDotNet.Serialization;

namespace BlogPostCreator.Services;

public sealed record ParsedMarkdown(IReadOnlyDictionary<string, object?> Data, string Body);

public static partial class FrontMatter
{
    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static ParsedMarkdown Parse(string text)
    {
        text = text.Replace("\r\n", "\n");
        var m = Block().Match(text);
        if (!m.Success) return new ParsedMarkdown(new Dictionary<string, object?>(), text);

        var yaml = new DeserializerBuilder().Build().Deserialize<Dictionary<string, object?>>(m.Groups[1].Value)
                   ?? new Dictionary<string, object?>();
        return new ParsedMarkdown(yaml, text[m.Length..]);
    }

    public static IReadOnlyList<string> AsList(object? value) => value switch
    {
        null => [],
        IEnumerable<object> items => items.Select(i => i?.ToString() ?? "").Where(s => s.Length > 0).ToList(),
        _ => [value.ToString()!],
    };

    /// <summary>
    /// Writes the drop-folder Markdown. Strings are JSON-quoted, which is valid YAML,
    /// the same convention import-blog.mjs uses for the files it writes.
    /// </summary>
    public static string Render(PostDraft post)
    {
        var lines = new List<string>
        {
            "---",
            $"title: {Q(post.Title)}",
            $"date: {post.Date:yyyy-MM-dd}",
            $"slug: {Q(post.Slug)}",
        };
        if (!string.IsNullOrWhiteSpace(post.Description)) lines.Add($"description: {Q(post.Description.Trim())}");
        lines.Add($"categories: [{string.Join(", ", post.Categories.Select(Q))}]");
        if (post.Tags.Count > 0) lines.Add($"tags: [{string.Join(", ", post.Tags.Select(Q))}]");
        if (post.Draft) lines.Add("draft: true");
        lines.Add("---");
        lines.Add("");
        return string.Join('\n', lines) + post.Body.Replace("\r\n", "\n").Trim() + "\n";
    }

    private static string Q(string s) => JsonSerializer.Serialize(s, Json);

    [GeneratedRegex(@"\A---[ \t]*\n(.*?)\n---[ \t]*(?:\n|\z)", RegexOptions.Singleline)]
    private static partial Regex Block();
}
