using BlogPostCreator.Models;

namespace BlogPostCreator.Services;

public sealed record ValidationResult(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)
{
    public bool Ok => Errors.Count == 0;
}

/// <summary>
/// Enforces the rules from incoming/blog/README.md locally, so a bad post fails here
/// instead of in the Import content workflow.
/// </summary>
public static class PostValidator
{
    private static readonly string[] FeaturedExtensions = [".png", ".jpg", ".jpeg", ".webp"];

    public static ValidationResult Validate(PostDraft post, RepoIndex repo)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(post.Title)) errors.Add("A title is required.");
        if (string.IsNullOrWhiteSpace(post.Slug)) errors.Add("The slug is empty. Give the post a title or set a slug.");
        else if (post.Slug != Slug.From(post.Slug)) errors.Add($"The slug \"{post.Slug}\" isn't normalized; it would import as \"{Slug.From(post.Slug)}\".");
        if (string.IsNullOrWhiteSpace(post.Body)) errors.Add("The post body is empty.");

        if (string.IsNullOrWhiteSpace(post.FeaturedImagePath)) errors.Add("A featured image is required.");
        else if (!File.Exists(post.FeaturedImagePath)) errors.Add($"Featured image not found: {post.FeaturedImagePath}");
        else if (!FeaturedExtensions.Contains(Path.GetExtension(post.FeaturedImagePath).ToLowerInvariant()))
            errors.Add("The featured image must be .png, .jpg, .jpeg, or .webp.");

        if (post.Categories.Count == 0) warnings.Add("No category picked; the importer will use \"blog\".");
        if (string.IsNullOrWhiteSpace(post.Description)) warnings.Add("No description; cards and RSS will use the first paragraph.");
        if (post.Draft) warnings.Add("Draft is on: the post is imported but hidden on the site.");

        // Slug collisions: same month updates in place, a different month fails the import.
        var month = $"{post.Date:yyyy}/{post.Date:MM}";
        if (!string.IsNullOrEmpty(post.Slug) && repo.Posts.TryGetValue(post.Slug, out var existing))
        {
            if (existing == month) warnings.Add($"A post with slug \"{post.Slug}\" already exists in {existing}; publishing will UPDATE it in place.");
            else errors.Add($"Slug \"{post.Slug}\" already exists in {existing}. Change the slug (or the date to {existing} to update that post).");
        }

        // Every local image the body references must travel with the post.
        var extraNames = post.ExtraImagePaths.Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        foreach (var r in MarkdownImages.LocalRefs(post.Body))
        {
            var name = Path.GetFileName(r);
            if (!extraNames.Contains(name)) errors.Add($"The body references \"{r}\" but no image named \"{name}\" is attached.");
        }
        foreach (var path in post.ExtraImagePaths)
        {
            if (!File.Exists(path)) errors.Add($"Attached image not found: {path}");
            if (Path.GetFileName(path) == post.FeaturedImageFileName) errors.Add($"Attached image \"{Path.GetFileName(path)}\" has the same name as the featured image.");
            if (string.Equals(Path.GetFileName(path), post.MarkdownFileName, StringComparison.OrdinalIgnoreCase)) errors.Add("An attached file has the same name as the post.");
        }
        var dupes = post.ExtraImagePaths.GroupBy(p => Path.GetFileName(p)).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var d in dupes) errors.Add($"Two attached images are both named \"{d}\".");

        return new ValidationResult(errors, warnings);
    }
}
