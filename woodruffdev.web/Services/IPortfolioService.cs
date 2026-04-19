using WoodruffDev.Models;

namespace WoodruffDev.Services;

public interface IPortfolioService
{
    IReadOnlyList<PortfolioItem> GetAll();
    IReadOnlyList<PortfolioItem> GetFeatured(int count);
    IReadOnlyList<PortfolioItem> GetByFilter(PortfolioFilter filter);
}
