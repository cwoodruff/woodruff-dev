namespace BlogPostCreator.Models;

/// <summary>Everything needed to produce incoming/blog/&lt;slug&gt;.md plus its images.</summary>
public sealed record PostDraft
{
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required DateOnly Date { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public bool Draft { get; init; }
    public required string Body { get; init; }
    public required string FeaturedImagePath { get; init; }

    /// <summary>Other images the body references by file name; copied next to the Markdown file.</summary>
    public IReadOnlyList<string> ExtraImagePaths { get; init; } = [];

    public string MarkdownFileName => $"{Slug}.md";
    public string FeaturedImageFileName => Slug + Path.GetExtension(FeaturedImagePath).ToLowerInvariant();
}
