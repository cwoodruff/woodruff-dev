using System.Diagnostics;

namespace BlogPostCreator.Services;

/// <summary>Best-effort OS notification so a long publish can run in the background.</summary>
public static class DesktopNotifier
{
    public static void Notify(string title, string message)
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo("osascript") { UseShellExecute = false, CreateNoWindow = true };
                psi.ArgumentList.Add("-e");
                psi.ArgumentList.Add($"display notification {AppleString(message)} with title {AppleString(title)}");
                Process.Start(psi);
            }
            else if (OperatingSystem.IsLinux())
            {
                var psi = new ProcessStartInfo("notify-send") { UseShellExecute = false, CreateNoWindow = true };
                psi.ArgumentList.Add(title);
                psi.ArgumentList.Add(message);
                Process.Start(psi);
            }
        }
        catch
        {
            // No notifier available; the window still shows the result.
        }
    }

    private static string AppleString(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            if (OperatingSystem.IsMacOS()) Process.Start("open", url);
            else if (OperatingSystem.IsLinux()) Process.Start("xdg-open", url);
        }
    }
}
