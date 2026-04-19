using Microsoft.AspNetCore.Mvc.RazorPages;
using WoodruffDev.Models;
using WoodruffDev.Services;

namespace WoodruffDev.Pages;

public class MediaModel : PageModel
{
    private readonly IMediaService _mediaService;

    public MediaModel(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public IReadOnlyList<MediaItem> Items { get; private set; } = [];

    public void OnGet()
    {
        Items = _mediaService.GetAll();
    }
}
