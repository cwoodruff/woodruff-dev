using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WoodruffDev.Pages.Services;

public class RetainerModel : PageModel
{
    private readonly ILogger<RetainerModel> _logger;

    public RetainerModel(ILogger<RetainerModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}
