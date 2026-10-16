using System.Text.Json;

namespace BlogPostCreator.Services;

public sealed class AppSettings
{
    public string? RepoPath { get; set; }
    public string SiteUrl { get; set; } = "https://woodruff.dev";

    private static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlogPostCreator");
    private static string FilePath => Path.Combine(Dir, "settings.json");
    public static string DraftPath => Path.Combine(Dir, "draft.json");

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(FilePath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new() : new();
        }
        catch
        {
            return new();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}

/// <summary>The form as typed, kept between launches so closing the app never loses a post.</summary>
public sealed class SavedDraft
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool SlugEdited { get; set; }
    public DateTime? Date { get; set; }
    public string Description { get; set; } = "";
    public List<string> Categories { get; set; } = [];
    public string Tags { get; set; } = "";
    public bool Draft { get; set; }
    public string Body { get; set; } = "";
    public string? FeaturedImagePath { get; set; }
    public List<string> ExtraImagePaths { get; set; } = [];

    public static SavedDraft? Load()
    {
        try
        {
            return File.Exists(AppSettings.DraftPath) ? JsonSerializer.Deserialize<SavedDraft>(File.ReadAllText(AppSettings.DraftPath)) : null;
        }
        catch
        {
            return null;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AppSettings.DraftPath)!);
        File.WriteAllText(AppSettings.DraftPath, JsonSerializer.Serialize(this));
    }

    public static void Clear()
    {
        if (File.Exists(AppSettings.DraftPath)) File.Delete(AppSettings.DraftPath);
    }
}
