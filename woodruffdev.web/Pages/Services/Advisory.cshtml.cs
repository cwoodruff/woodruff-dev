using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WoodruffDev.Pages.Services;

public class AdvisoryModel : PageModel
{
    private readonly ILogger<AdvisoryModel> _logger;

    public AdvisoryModel(ILogger<AdvisoryModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}
