using Microsoft.AspNetCore.Mvc.RazorPages;
using WoodruffDev.Models;
using WoodruffDev.Services;

namespace WoodruffDev.Pages;

public class IndexModel : PageModel
{
    private readonly IBlogService _blogService;

    public IndexModel(IBlogService blogService)
    {
        _blogService = blogService;
    }

    public IReadOnlyList<BlogPost> RecentPosts { get; private set; } = [];

    public void OnGet()
    {
        RecentPosts = _blogService.GetRecentPosts(3);
    }
}
