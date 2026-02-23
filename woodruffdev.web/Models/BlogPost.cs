namespace WoodruffDev.Models;

public class BlogPost
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public string FeatureImagePath { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
}
