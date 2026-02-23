using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WoodruffDev.Pages.Services;

public class ExpertWitnessModel : PageModel
{
    private readonly ILogger<ExpertWitnessModel> _logger;

    public ExpertWitnessModel(ILogger<ExpertWitnessModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}
