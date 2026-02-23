using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WoodruffDev.Pages.Services;

public class FractionalArchitectModel : PageModel
{
    private readonly ILogger<FractionalArchitectModel> _logger;

    public FractionalArchitectModel(ILogger<FractionalArchitectModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}
