using Avalonia;
using Avalonia.Media;

namespace BlogPostCreator;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Plus Jakarta Sans is the site's typeface (site/src/styles/tokens.css --font-sans).
            .With(new FontManagerOptions
            {
                DefaultFamilyName = "avares://BlogPostCreator/Assets/Fonts#Plus Jakarta Sans",
            })
            .LogToTrace();
}
