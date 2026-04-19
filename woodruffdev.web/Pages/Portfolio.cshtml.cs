using Microsoft.AspNetCore.Mvc.RazorPages;
using WoodruffDev.Models;
using WoodruffDev.Services;

namespace WoodruffDev.Pages;

public class PortfolioModel : PageModel
{
    private readonly IPortfolioService _portfolioService;

    public PortfolioModel(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    public IReadOnlyList<PortfolioItem> Items { get; private set; } = [];

    public void OnGet()
    {
        Items = _portfolioService.GetAll();
    }
}
