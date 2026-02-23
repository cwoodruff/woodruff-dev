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

        foreach (var file in Directory.GetFiles(blogDir, "*.md"))
        {
            var content = File.ReadAllText(file);
            var slug = Path.GetFileNameWithoutExtension(file);

            var post = ParsePost(content, slug, pipeline, deserializer);
            if (post is not null)
                posts.Add(post);
        }

        return posts.OrderByDescending(p => p.PublishedDate).ToList();
    }

    private static BlogPost? ParsePost(
        string content, string slug, MarkdownPipeline pipeline, IDeserializer deserializer)
    {
        var match = FrontMatterRegex().Match(content);
        if (!match.Success)
            return null;

        var yaml = match.Groups[1].Value;
        var markdown = content[match.Length..].TrimStart();

        var frontMatter = deserializer.Deserialize<BlogPostFrontMatter>(yaml);

        // Extract title from first # heading
        var titleMatch = TitleRegex().Match(markdown);
        var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : slug;

        // Extract subtitle from first *...* line after the title
        string? subtitle = null;
        if (titleMatch.Success)
        {
            var afterTitle = markdown[(titleMatch.Index + titleMatch.Length)..].TrimStart();
            var subtitleMatch = SubtitleRegex().Match(afterTitle);
            if (subtitleMatch.Success)
                subtitle = subtitleMatch.Groups[1].Value.Trim();
        }

        var html = Markdown.ToHtml(markdown, pipeline);

        return new BlogPost
        {
            Slug = slug,
            Title = title,
            Subtitle = subtitle,
            Description = frontMatter.Description,
            Category = frontMatter.Category,
            PublishedDate = frontMatter.Date,
            FeatureImagePath = $"/images/blog-posts/{slug}.png",
            HtmlContent = html
        };
    }

    [GeneratedRegex(@"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline)]
    private static partial Regex FrontMatterRegex();

    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"^\*(.+)\*\s*$", RegexOptions.Multiline)]
    private static partial Regex SubtitleRegex();
}
