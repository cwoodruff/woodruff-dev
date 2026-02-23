using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WoodruffDev.Pages.Services;

public class MicroConsultingModel : PageModel
{
    private readonly ILogger<MicroConsultingModel> _logger;

    public MicroConsultingModel(ILogger<MicroConsultingModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}
