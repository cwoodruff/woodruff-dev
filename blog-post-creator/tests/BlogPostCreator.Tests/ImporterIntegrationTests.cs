using BlogPostCreator.Services;

namespace BlogPostCreator.Tests;

/// <summary>
/// Feeds the app's output to the site's real import-blog.mjs in a scratch copy of the
/// repo layout, proving the two agree. Skipped when the repo or node isn't available.
/// </summary>
public class ImporterIntegrationTests
{
    [Fact]
    public async Task Real_importer_accepts_the_apps_output()
    {
        var repo = RepoIndex.Locate(AppContext.BaseDirectory);
        if (repo is null || !Directory.Exists(Path.Combine(repo, "site", "node_modules", "gray-matter"))) return;

        var tmp = Directory.CreateTempSubdirectory("bpc-import-").FullName;
        try
        {
            var site = Path.Combine(tmp, "site");
            CopyDir(Path.Combine(repo, "site", "scripts"), Path.Combine(site, "scripts"));
            File.CreateSymbolicLink(Path.Combine(site, "node_modules"), Path.Combine(repo, "site", "node_modules"));
            File.Copy(Path.Combine(repo, "site", "package.json"), Path.Combine(site, "package.json"));
            Directory.CreateDirectory(Path.Combine(site, "src", "content", "blog"));
            var incoming = Directory.CreateDirectory(Path.Combine(tmp, "incoming", "blog")).FullName;

            var post = FrontMatterTests.Sample() with { Title = "What’s New: \"Quotes\" & Colons", Slug = Slug.From("What’s New: \"Quotes\" & Colons") };
            File.WriteAllText(Path.Combine(incoming, post.MarkdownFileName), FrontMatter.Render(post));
            File.WriteAllBytes(Path.Combine(incoming, post.Slug + ".png"), [0]);
            File.WriteAllBytes(Path.Combine(incoming, "diagram-1.png"), [0]);

            var result = await ProcessRunner.RunAsync("node", ["scripts/import-blog.mjs"], site);
            Assert.True(result.Ok, result.Combined);

            var index = Path.Combine(site, "src", "content", "blog", "2026", "10", post.Slug, "index.md");
            Assert.True(File.Exists(index), result.Combined);
            var parsed = FrontMatter.Parse(File.ReadAllText(index));
            Assert.Equal(post.Title, parsed.Data["title"]);
            Assert.Equal($"./images/{post.Slug}.png", parsed.Data["coverImage"]);
            Assert.Equal(["patterns"], FrontMatter.AsList(parsed.Data["categories"]));
            Assert.Contains("](images/diagram-1.png)", parsed.Body);
            Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(index)!, "images", "diagram-1.png")));
            Assert.Empty(Directory.GetFiles(incoming));
        }
        finally
        {
            Directory.Delete(tmp, true);
        }
    }

    private static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
        foreach (var d in Directory.GetDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
    }
}
