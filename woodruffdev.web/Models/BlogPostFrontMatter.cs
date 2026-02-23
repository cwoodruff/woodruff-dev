using YamlDotNet.Serialization;

namespace WoodruffDev.Models;

public class BlogPostFrontMatter
{
    [YamlMember(Alias = "category")]
    public string Category { get; set; } = string.Empty;

    [YamlMember(Alias = "date")]
    public DateTime Date { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;
}
