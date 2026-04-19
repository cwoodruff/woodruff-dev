using WoodruffDev.Models;

namespace WoodruffDev.Services;

public class MediaService : IMediaService
{
    private readonly List<MediaItem> _items =
    [
        new()
        {
            Title = "The Breakpoint Show",
            Type = MediaType.Podcast,
            Outlet = "Hosted by Chris Woodruff",
            Url = "https://thebreakpoint.show/",
            Description = "Conversations with software engineers, architects, and leaders about the decisions that shape systems and careers."
        },
        new()
        {
            Title = "The Woody Show",
            Type = MediaType.Podcast,
            Outlet = "Hosted by Chris Woodruff",
            Url = "https://thewoodyshow.com/",
            Description = "A long-running podcast exploring the intersection of software craft, culture, and the lives of the people building today&rsquo;s systems."
        },
        new()
        {
            Title = "Chris Woodruff on YouTube",
            Type = MediaType.Video,
            Outlet = "YouTube",
            Url = "https://www.youtube.com/@ChrisWoodruff",
            Description = "Conference talks, workshop recordings, and deep-dive videos on .NET, architecture, and software design."
        }
    ];

    public IReadOnlyList<MediaItem> GetAll() =>
        _items
            .OrderByDescending(i => i.Date ?? DateOnly.MinValue)
            .ToList();

    public IReadOnlyList<MediaItem> GetByType(MediaType type) =>
        GetAll().Where(i => i.Type == type).ToList();
}
