using System.Text.RegularExpressions;
using Markdig;
using WoodruffDev.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WoodruffDev.Services;

public partial class BlogService : IBlogService
{
    private readonly List<BlogPost> _posts;

    public BlogService(IWebHostEnvironment env)
    {
        var blogDir = Path.Combine(env.ContentRootPath, "BlogPosts");
        _posts = LoadPosts(blogDir);
    }

    public IReadOnlyList<BlogPost> GetAllPosts() => _posts;

    public IReadOnlyList<BlogPost> GetRecentPosts(int count) =>
        _posts.Take(count).ToList();

    public BlogPost? GetPostBySlug(string slug) =>
        _posts.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    private static List<BlogPost> LoadPosts(string blogDir)
    {
        if (!Directory.Exists(blogDir))
            return [];

        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var posts = new List<BlogPost>();

        // Recursively find all index.md files in the year/month/slug/ structure
        foreach (var file in Directory.GetFiles(blogDir, "index.md", SearchOption.AllDirectories))
        {
            var postDir = Path.GetDirectoryName(file)!;
            var slug = Path.GetFileName(postDir);

            // Build relative path from blogDir to the post directory for image URLs
            // e.g. "2023/02/accelerating-ef-core-with-compiled-queries"
            var relativePath = Path.GetRelativePath(blogDir, postDir).Replace('\\', '/');

            var content = File.ReadAllText(file);

            var post = ParsePost(content, slug, relativePath, pipeline, deserializer);
            if (post is not null)
                posts.Add(post);
        }

        return posts.OrderByDescending(p => p.PublishedDate).ToList();
    }

    private static BlogPost? ParsePost(
        string content, string slug, string relativePath,
        MarkdownPipeline pipeline, IDeserializer deserializer)
    {
        var match = FrontMatterRegex().Match(content);
        if (!match.Success)
            return null;

        var yaml = match.Groups[1].Value;
        var markdown = content[match.Length..].TrimStart();

        var frontMatter = deserializer.Deserialize<BlogPostFrontMatter>(yaml);

        // Use title from front matter
        var title = !string.IsNullOrWhiteSpace(frontMatter.Title)
            ? frontMatter.Title
            : slug;

        // Use first category from the categories list
        var category = frontMatter.Categories?.FirstOrDefault() ?? string.Empty;

        // Build feature image path from coverImage front matter field
        var featureImagePath = !string.IsNullOrWhiteSpace(frontMatter.CoverImage)
            ? $"/blog-content/{relativePath}/images/{frontMatter.CoverImage}"
            : string.Empty;

        // Generate description from the first paragraph of markdown content
        var description = ExtractDescription(markdown);

        // Convert markdown to HTML
        var html = Markdown.ToHtml(markdown, pipeline);

        // Rewrite relative image paths (images/...) to absolute paths
        var imageBasePath = $"/blog-content/{relativePath}/";
        html = RelativeImageRegex().Replace(html, m =>
        {
            var prefix = m.Groups[1].Value;    // src=" or src='
            var imgPath = m.Groups[2].Value;   // images/...
            var suffix = m.Groups[3].Value;    // " or '
            return $"{prefix}{imageBasePath}{imgPath}{suffix}";
        });

        return new BlogPost
        {
            Slug = slug,
            Title = title,
            Description = description,
            Category = category,
            PublishedDate = frontMatter.Date,
            FeatureImagePath = featureImagePath,
            HtmlContent = html
        };
    }

    private static string ExtractDescription(string markdown)
    {
        // Strip the <!--more--> marker and everything after it for description purposes
        var moreIndex = markdown.IndexOf("<!--more-->", StringComparison.OrdinalIgnoreCase);
        var source = moreIndex > 0 ? markdown[..moreIndex] : markdown;

        // Remove markdown formatting for a clean plaintext excerpt
        var plainText = MarkdownStrippingRegex().Replace(source, "").Trim();

        // Remove blockquote markers
        plainText = BlockquoteRegex().Replace(plainText, "").Trim();

        // Collapse whitespace
        plainText = WhitespaceRegex().Replace(plainText, " ").Trim();

        if (plainText.Length <= 200)
            return plainText;

        // Truncate at a word boundary
        var truncated = plainText[..200];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > 100)
            truncated = truncated[..lastSpace];

        return truncated + "...";
    }

    [GeneratedRegex(@"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline)]
    private static partial Regex FrontMatterRegex();

    // Match src="images/..." or src='images/...' in generated HTML
    [GeneratedRegex(@"(src=[""'])(?!https?://|/)(images/[^""']+)([""'])")]
    private static partial Regex RelativeImageRegex();

    // Strip common markdown syntax for plaintext description
    [GeneratedRegex(@"(\*{1,2}|_{1,2}|`{1,3}|#{1,6}\s|!\[.*?\]\(.*?\)|\[|\]\(.*?\))")]
    private static partial Regex MarkdownStrippingRegex();

    [GeneratedRegex(@"^>\s?", RegexOptions.Multiline)]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
