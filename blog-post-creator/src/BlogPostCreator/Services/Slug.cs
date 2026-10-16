using System.Text;
using System.Text.RegularExpressions;

namespace BlogPostCreator.Services;

/// <summary>Port of slugify() in site/scripts/lib/import-utils.mjs; must stay compatible with it.</summary>
public static partial class Slug
{
    public static string From(string text)
    {
        var s = text.Normalize(NormalizationForm.FormKD);
        s = CombiningMarks().Replace(s, "");
        s = Quotes().Replace(s, "");
        s = s.ToLowerInvariant();
        s = NonAlnum().Replace(s, "-");
        s = s.Trim('-');
        return s.Length > 120 ? s[..120] : s;
    }

    [GeneratedRegex(@"[̀-ͯ]")]
    private static partial Regex CombiningMarks();

    [GeneratedRegex(@"[’'""]")]
    private static partial Regex Quotes();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlnum();
}
