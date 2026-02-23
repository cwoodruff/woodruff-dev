using Microsoft.AspNetCore.Mvc.RazorPages;
using WoodruffDev.Models;
using WoodruffDev.Services;

namespace WoodruffDev.Pages;

public class BlogModel : PageModel
{
    private readonly IBlogService _blogService;

    public BlogModel(IBlogService blogService)
    {
        _blogService = blogService;
    }

    public IReadOnlyList<BlogPost> Posts { get; private set; } = [];

    public void OnGet()
    {
        Posts = _blogService.GetAllPosts();
    }
}
