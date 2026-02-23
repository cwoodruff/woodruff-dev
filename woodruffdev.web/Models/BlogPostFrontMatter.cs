using YamlDotNet.Serialization;

namespace WoodruffDev.Models;

public class BlogPostFrontMatter
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "date")]
    public DateTime Date { get; set; }

    [YamlMember(Alias = "categories")]
    public List<string> Categories { get; set; } = [];

    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = [];

    [YamlMember(Alias = "coverImage")]
    public string CoverImage { get; set; } = string.Empty;
}
