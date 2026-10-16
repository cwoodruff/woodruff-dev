namespace BlogPostCreator.Services;

/// <summary>What already exists in the site repo: post slugs (for collision checks) and category usage.</summary>
public sealed class RepoIndex
{
    /// <summary>Categories documented in incoming/blog/README.md, offered even if no post uses them yet.</summary>
    public static readonly string[] DocumentedCategories =
    [
        "patterns", "efcore", "htmx", "rust", "terraform", "http-rest", "genetic-algorithms",
        "network-book-sample", "biz-software", "simplicity-first", "speaking", "ai", "blog",
    ];

    public required string RepoRoot { get; init; }

    /// <summary>slug → "YYYY/MM" folder the post lives in.</summary>
    public required IReadOnlyDictionary<string, string> Posts { get; init; }

    /// <summary>Category → number of posts, most-used first.</summary>
    public required IReadOnlyList<KeyValuePair<string, int>> Categories { get; init; }

    public string BlogContentDir => Path.Combine(RepoRoot, "site", "src", "content", "blog");
    public string IncomingBlogDir => Path.Combine(RepoRoot, "incoming", "blog");

    public static bool LooksLikeSiteRepo(string dir) =>
        File.Exists(Path.Combine(dir, "incoming", "blog", "README.md")) &&
        Directory.Exists(Path.Combine(dir, "site", "src", "content", "blog")) &&
        File.Exists(Path.Combine(dir, "site", "scripts", "import-blog.mjs"));

    /// <summary>Walks up from the app's location and the working directory looking for the site repo.</summary>
    public static string? Locate(params string?[] hints)
    {
        foreach (var start in hints.Concat([AppContext.BaseDirectory, Environment.CurrentDirectory]))
        {
            for (var dir = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start); dir is not null; dir = dir.Parent)
                if (LooksLikeSiteRepo(dir.FullName)) return dir.FullName;
        }
        return null;
    }

    public static RepoIndex Load(string repoRoot)
    {
        var blog = Path.Combine(repoRoot, "site", "src", "content", "blog");
        var posts = new Dictionary<string, string>(StringComparer.Ordinal);
        var counts = DocumentedCategories.ToDictionary(c => c, _ => 0, StringComparer.Ordinal);

        foreach (var year in Directory.EnumerateDirectories(blog))
        foreach (var month in Directory.EnumerateDirectories(year))
        foreach (var postDir in Directory.EnumerateDirectories(month))
        {
            var index = Path.Combine(postDir, "index.md");
            if (!File.Exists(index)) continue;
            posts[Path.GetFileName(postDir)] = $"{Path.GetFileName(year)}/{Path.GetFileName(month)}";
            try
            {
                var fm = FrontMatter.Parse(File.ReadAllText(index)).Data;
                if (fm.TryGetValue("categories", out var cats))
                    foreach (var c in FrontMatter.AsList(cats))
                        counts[c] = counts.GetValueOrDefault(c) + 1;
            }
            catch
            {
                // A post with odd front matter only costs us its categories in the picker.
            }
        }

        return new RepoIndex
        {
            RepoRoot = repoRoot,
            Posts = posts,
            Categories = counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList(),
        };
    }
}
