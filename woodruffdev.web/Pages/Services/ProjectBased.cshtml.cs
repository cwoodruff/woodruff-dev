using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WoodruffDev.Pages.Services;

public class ProjectBasedModel : PageModel
{
    private readonly ILogger<ProjectBasedModel> _logger;

    public ProjectBasedModel(ILogger<ProjectBasedModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}
