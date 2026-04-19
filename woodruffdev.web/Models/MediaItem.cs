namespace WoodruffDev.Models;

public enum MediaType
{
    Podcast,
    Video,
    Article
}

public class MediaItem
{
    public string Title { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public string Outlet { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateOnly? Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Duration { get; set; }
    public string? ThumbnailUrl { get; set; }

    public string TypeLabel => Type switch
    {
        MediaType.Podcast => "Podcast",
        MediaType.Video => "Video",
        MediaType.Article => "Article",
        _ => Type.ToString()
    };

    public string FilterKey => Type switch
    {
        MediaType.Podcast => "podcasts",
        MediaType.Video => "videos",
        MediaType.Article => "articles",
        _ => "all"
    };
}
