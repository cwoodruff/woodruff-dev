using WoodruffDev.Models;

namespace WoodruffDev.Services;

public class PortfolioService : IPortfolioService
{
    private readonly List<PortfolioItem> _items =
    [
        new()
        {
            Title = "ASP.NET Core Reimagined with htmx",
            Category = PortfolioCategory.Book,
            Description = "A practical guide to building modern web applications with ASP.NET Core and htmx — leveraging hypermedia-driven interactions without heavy JavaScript frameworks.",
            InternalSlug = "/Projects/AspNetCoreHtmxBook",
            ImagePath = "/images/project-htmx-book.jpg",
            Tech = ["ASP.NET Core", "htmx", "Razor Pages"],
            Year = 2025,
            Featured = true
        },
        new()
        {
            Title = "Entity Framework Core Course",
            Category = PortfolioCategory.Course,
            Description = "A deep-dive course on Entity Framework Core — modeling, migrations, performance tuning, and real-world patterns for data-access in .NET.",
            InternalSlug = "/Projects/EntityFrameworkCoreCourse",
            ImagePath = "/images/project-ef-core.jpg",
            Tech = [".NET", "EF Core", "SQL"],
            Year = 2024,
            Featured = true
        },
        new()
        {
            Title = "htmx & Razor Pages Workshop",
            Category = PortfolioCategory.Workshop,
            Description = "A hands-on workshop walking teams through building production-ready htmx features on top of ASP.NET Core Razor Pages.",
            InternalSlug = "/Projects/HtmxRazorPagesWorkshop",
            ImagePath = "/images/project-htmx-workshop.jpg",
            Tech = ["htmx", "Razor Pages"],
            Year = 2024
        },
        new()
        {
            Title = "Terraform Workshop",
            Category = PortfolioCategory.Workshop,
            Description = "Infrastructure-as-code fundamentals — designing reusable Terraform modules, state management, and multi-environment deployment pipelines.",
            InternalSlug = "/Projects/TerraformWorkshop",
            ImagePath = "/images/project-terraform.jpg",
            Tech = ["Terraform", "Azure", "AWS"],
            Year = 2024
        },
        new()
        {
            Title = "cwoodruff on GitHub",
            Category = PortfolioCategory.OpenSource,
            Description = "My open-source work — sample projects, conference demos, and libraries across .NET, data access, and web architecture.",
            Url = "https://github.com/cwoodruff",
            ImagePath = "/images/project-github.jpg",
            Tech = [".NET", "C#", "Open Source"],
            Featured = true
        }
    ];

    public IReadOnlyList<PortfolioItem> GetAll() =>
        _items
            .OrderByDescending(i => i.Featured)
            .ThenByDescending(i => i.Year ?? 0)
            .ToList();

    public IReadOnlyList<PortfolioItem> GetFeatured(int count) =>
        _items
            .Where(i => i.Featured)
            .Take(count)
            .ToList();

    public IReadOnlyList<PortfolioItem> GetByFilter(PortfolioFilter filter) =>
        filter switch
        {
            PortfolioFilter.OpenSource => GetAll()
                .Where(i => i.Category == PortfolioCategory.OpenSource)
                .ToList(),
            PortfolioFilter.Writing => GetAll()
                .Where(i => i.IsWriting)
                .ToList(),
            _ => GetAll()
        };
}
