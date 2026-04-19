namespace WoodruffDev.Models;

public enum PortfolioCategory
{
    OpenSource,
    Book,
    Course,
    Workshop,
    Article
}

public enum PortfolioFilter
{
    All,
    OpenSource,
    Writing
}

public class PortfolioItem
{
    public string Title { get; set; } = string.Empty;
    public PortfolioCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? InternalSlug { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string[] Tech { get; set; } = [];
    public int? Year { get; set; }
    public bool Featured { get; set; }

    public string CategoryLabel => Category switch
    {
        PortfolioCategory.OpenSource => "Open Source",
        PortfolioCategory.Book => "Book",
        PortfolioCategory.Course => "Course",
        PortfolioCategory.Workshop => "Workshop",
        PortfolioCategory.Article => "Article",
        _ => Category.ToString()
    };

    public bool IsWriting => Category is PortfolioCategory.Book
        or PortfolioCategory.Course
        or PortfolioCategory.Workshop
        or PortfolioCategory.Article;

    public bool IsExternal => !string.IsNullOrWhiteSpace(Url)
        && string.IsNullOrWhiteSpace(InternalSlug);

    public string Href => !string.IsNullOrWhiteSpace(InternalSlug)
        ? InternalSlug
        : Url ?? "#";
}
