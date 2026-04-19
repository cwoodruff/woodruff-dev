using WoodruffDev.Models;

namespace WoodruffDev.Services;

public interface IMediaService
{
    IReadOnlyList<MediaItem> GetAll();
    IReadOnlyList<MediaItem> GetByType(MediaType type);
}
