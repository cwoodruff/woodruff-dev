using WoodruffDev.Models;

namespace WoodruffDev.Services;

public interface IBlogService
{
    IReadOnlyList<BlogPost> GetAllPosts();
    IReadOnlyList<BlogPost> GetRecentPosts(int count);
    BlogPost? GetPostBySlug(string slug);
}
