using Microsoft.AspNetCore.Mvc.RazorPages;
using WoodruffDev.Models;
using WoodruffDev.Services;

namespace WoodruffDev.Pages;

public class IndexModel : PageModel
{
    private readonly IBlogService _blogService;
    private readonly IPortfolioService _portfolioService;

    public IndexModel(IBlogService blogService, IPortfolioService portfolioService)
    {
        _blogService = blogService;
        _portfolioService = portfolioService;
    }

    public IReadOnlyList<BlogPost> RecentPosts { get; private set; } = [];
    public IReadOnlyList<PortfolioItem> FeaturedProjects { get; private set; } = [];

    public void OnGet()
    {
        RecentPosts = _blogService.GetRecentPosts(3);
        FeaturedProjects = _portfolioService.GetFeatured(3);
    }
}
