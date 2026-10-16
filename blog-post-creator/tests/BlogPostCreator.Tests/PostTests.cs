using BlogPostCreator.Models;
using BlogPostCreator.Services;

namespace BlogPostCreator.Tests;

public class SlugTests
{
    [Theory]
    [InlineData("Leader Election in .NET: Picking One Boss Without Creating Two", "leader-election-in-net-picking-one-boss-without-creating-two")]
    [InlineData("What’s New in C# 15?", "whats-new-in-c-15")]
    [InlineData("  Café crème -- déjà vu  ", "cafe-creme-deja-vu")]
    [InlineData("\"Quoted\" 'single'", "quoted-single")]
    public void Matches_importer_slugify(string title, string expected) => Assert.Equal(expected, Slug.From(title));
}

public class FrontMatterTests
{
    [Fact]
    public void Render_round_trips_through_parse()
    {
        var post = Sample() with { Title = "A \"quoted\" title: with colon", Description = "Line one\nline two", Draft = true };
        var parsed = FrontMatter.Parse(FrontMatter.Render(post));

        Assert.Equal(post.Title, parsed.Data["title"]);
        Assert.Equal("2026-10-06", parsed.Data["date"]);
        Assert.Equal(post.Slug, parsed.Data["slug"]);
        Assert.Equal(post.Description, parsed.Data["description"]);
        Assert.Equal(["patterns"], FrontMatter.AsList(parsed.Data["categories"]));
        Assert.Equal(["dotnet", "csharp"], FrontMatter.AsList(parsed.Data["tags"]));
        Assert.Equal("true", parsed.Data["draft"]);
        Assert.StartsWith("Body text", parsed.Body);
    }

    [Fact]
    public void Parse_without_front_matter_returns_whole_body()
    {
        var parsed = FrontMatter.Parse("# Hello\n\nWorld");
        Assert.Empty(parsed.Data);
        Assert.Equal("# Hello\n\nWorld", parsed.Body);
    }

    internal static PostDraft Sample() => new()
    {
        Title = "Leader Election",
        Slug = "leader-election",
        Date = new DateOnly(2026, 10, 6),
        Description = "Summary.",
        Categories = ["patterns"],
        Tags = ["dotnet", "csharp"],
        Body = "Body text\n\n![Lease diagram](diagram-1.png)\n",
        FeaturedImagePath = "",
    };
}

public class ValidatorTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("bpc-").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private string Touch(string name)
    {
        var p = Path.Combine(_dir, name);
        File.WriteAllBytes(p, [0]);
        return p;
    }

    private static RepoIndex Repo(params (string Slug, string Month)[] posts) => new()
    {
        RepoRoot = "/repo",
        Posts = posts.ToDictionary(p => p.Slug, p => p.Month),
        Categories = [],
    };

    private PostDraft Valid() => FrontMatterTests.Sample() with
    {
        FeaturedImagePath = Touch("cover.png"),
        ExtraImagePaths = [Touch("diagram-1.png")],
    };

    [Fact]
    public void Valid_post_passes() => Assert.True(PostValidator.Validate(Valid(), Repo()).Ok);

    [Fact]
    public void Slug_in_another_month_is_an_error()
    {
        var r = PostValidator.Validate(Valid(), Repo(("leader-election", "2025/02")));
        Assert.Contains(r.Errors, e => e.Contains("already exists in 2025/02"));
    }

    [Fact]
    public void Slug_in_same_month_is_an_update_warning()
    {
        var r = PostValidator.Validate(Valid(), Repo(("leader-election", "2026/10")));
        Assert.True(r.Ok);
        Assert.Contains(r.Warnings, w => w.Contains("UPDATE"));
    }

    [Fact]
    public void Referenced_image_must_be_attached()
    {
        var r = PostValidator.Validate(Valid() with { ExtraImagePaths = [] }, Repo());
        Assert.Contains(r.Errors, e => e.Contains("diagram-1.png"));
    }

    [Fact]
    public void Featured_image_must_be_a_supported_type()
    {
        var r = PostValidator.Validate(Valid() with { FeaturedImagePath = Touch("cover.gif") }, Repo());
        Assert.Contains(r.Errors, e => e.Contains(".png, .jpg"));
    }

    [Fact]
    public void Unnormalized_slug_is_an_error()
    {
        var r = PostValidator.Validate(Valid() with { Slug = "Leader Election" }, Repo());
        Assert.Contains(r.Errors, e => e.Contains("isn't normalized"));
    }
}
