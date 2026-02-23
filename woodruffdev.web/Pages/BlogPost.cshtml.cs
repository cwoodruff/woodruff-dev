using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WoodruffDev.Models;
using WoodruffDev.Services;

namespace WoodruffDev.Pages;

public class BlogPostModel : PageModel
{
    private readonly IBlogService _blogService;

    public BlogPostModel(IBlogService blogService)
    {
        _blogService = blogService;
    }

    public BlogPost Post { get; private set; } = default!;

    public IActionResult OnGet(string slug)
    {
        var post = _blogService.GetPostBySlug(slug);
        if (post is null)
            return NotFound();

        Post = post;
        return Page();
    }
}
