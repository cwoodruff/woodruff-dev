using System.Diagnostics;
using System.Text;

namespace BlogPostCreator.Services;

public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr)
{
    public bool Ok => ExitCode == 0;
    public string Combined => string.Join('\n', new[] { StdOut.Trim(), StdErr.Trim() }.Where(s => s.Length > 0));
}

public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(string file, IEnumerable<string> args, string workingDir, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(file)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        // Never let git/gh block on an interactive prompt behind the UI.
        psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
        psi.Environment["GH_PROMPT_DISABLED"] = "1";
        psi.Environment["NO_COLOR"] = "1";
        PrependCommonToolPaths(psi);

        using var p = new Process { StartInfo = psi };
        try
        {
            p.Start();
        }
        catch (Exception ex)
        {
            return new ProcessResult(-1, "", $"Could not start '{file}': {ex.Message}");
        }

        var stdout = p.StandardOutput.ReadToEndAsync(ct);
        var stderr = p.StandardError.ReadToEndAsync(ct);
        try
        {
            await p.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { p.Kill(entireProcessTree: true); } catch { /* already gone */ }
            throw;
        }
        return new ProcessResult(p.ExitCode, await stdout, await stderr);
    }

    /// <summary>
    /// Apps launched from Finder or a desktop launcher get a minimal PATH that
    /// misses Homebrew, where git and gh usually live on macOS. Appended, so the
    /// user's own PATH order still wins.
    /// </summary>
    private static void PrependCommonToolPaths(ProcessStartInfo psi)
    {
        if (OperatingSystem.IsWindows()) return;
        var extra = new[] { "/opt/homebrew/bin", "/usr/local/bin", "/usr/bin", "/bin" };
        var current = psi.Environment.TryGetValue("PATH", out var p) ? p ?? "" : "";
        var parts = current.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
        foreach (var dir in extra)
            if (!parts.Contains(dir)) parts.Add(dir);
        psi.Environment["PATH"] = string.Join(':', parts);
    }
}
