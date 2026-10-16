using System.Net;
using System.Text.Json;
using BlogPostCreator.Models;

namespace BlogPostCreator.Services;

/// <summary>
/// Runs the documented publishing process end to end:
/// drop the post into incoming/blog/, push to main, follow the Import content
/// workflow through import, build, and deploy, then confirm the page is live.
/// </summary>
public sealed class PublishPipeline(IPipelineReporter report)
{
    private const string Workflow = "import-content.yml";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private string _repo = "";

    public async Task<PublishOutcome> RunAsync(PostDraft post, PipelineOptions options, CancellationToken ct)
    {
        _repo = options.RepoRoot;
        var current = StepId.Repository;
        string? runUrl = null;
        try
        {
            current = StepId.Repository;
            await CheckRepositoryAsync(ct);

            current = StepId.Sync;
            await SyncAsync(ct);

            current = StepId.Validate;
            Validate(post);

            if (options.CheckOnly)
            {
                foreach (var id in Enum.GetValues<StepId>().Where(s => s > StepId.Validate))
                    report.Step(id, StepState.Skipped, "Check only");
                report.Log("Check passed. Nothing was written or pushed.", LogLevel.Success);
                return new PublishOutcome(true, null, null, "Check passed. The post is ready to publish.");
            }

            current = StepId.Write;
            var written = WriteFiles(post);

            current = StepId.Push;
            var sha = await CommitAndPushAsync(post, written, ct);

            current = StepId.WorkflowQueued;
            var runId = await WaitForRunAsync(sha, ct);

            current = StepId.ImportBuild;
            runUrl = await FollowRunAsync(runId, ct);

            var postUrl = $"{options.SiteUrl.TrimEnd('/')}/blog/{post.Slug}/";
            current = StepId.Live;
            if (post.Draft)
            {
                report.Step(StepId.Live, StepState.Skipped, "Draft posts are hidden on the site");
            }
            else
            {
                await WaitForLiveAsync(postUrl, ct);
            }

            current = StepId.PullBack;
            await PullBackAsync(ct);

            var summary = post.Draft
                ? $"Imported \"{post.Title}\" as a draft."
                : $"\"{post.Title}\" is live.";
            report.Log(summary, LogLevel.Success);
            return new PublishOutcome(true, post.Draft ? null : postUrl, runUrl, summary);
        }
        catch (OperationCanceledException)
        {
            report.Step(current, StepState.Failed, "Cancelled");
            var msg = current >= StepId.WorkflowQueued
                ? "Stopped watching. The post was already pushed; the workflow keeps running on GitHub."
                : "Cancelled.";
            report.Log(msg, LogLevel.Warning);
            return new PublishOutcome(false, null, runUrl, msg);
        }
        catch (PipelineFailedException ex)
        {
            report.Step(current, StepState.Failed, ex.Message);
            report.Log(ex.Message, LogLevel.Error);
            return new PublishOutcome(false, null, runUrl, ex.Message);
        }
        catch (Exception ex)
        {
            report.Step(current, StepState.Failed, ex.Message);
            report.Log($"Unexpected error: {ex}", LogLevel.Error);
            return new PublishOutcome(false, null, runUrl, ex.Message);
        }
    }

    // ---------------------------------------------------------------- steps

    private async Task CheckRepositoryAsync(CancellationToken ct)
    {
        report.Step(StepId.Repository, StepState.Running, "Checking git, gh, and the branch");

        var git = await Run("git", ["--version"], ct, quiet: true);
        if (!git.Ok) throw new PipelineFailedException("git was not found on PATH.");
        var gh = await Run("gh", ["--version"], ct, quiet: true);
        if (!gh.Ok) throw new PipelineFailedException("The GitHub CLI (gh) was not found. Install it from https://cli.github.com and run `gh auth login`.");
        var auth = await Run("gh", ["auth", "status"], ct, quiet: true);
        if (!auth.Ok) throw new PipelineFailedException("gh is not signed in. Run `gh auth login` in a terminal, then try again.");

        var branch = (await Must("git", ["rev-parse", "--abbrev-ref", "HEAD"], ct)).StdOut.Trim();
        if (branch != "main")
            throw new PipelineFailedException($"The repo is on branch \"{branch}\". Switch to main first; the import only runs on pushes to main.");

        var pending = (await Must("git", ["ls-files", "--", "incoming/blog"], ct)).StdOut
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(f => !f.EndsWith("/README.md", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (pending.Count > 0)
            throw new PipelineFailedException($"incoming/blog/ already has committed files waiting for import ({string.Join(", ", pending)}). Let that import finish or clean it up first.");

        report.Step(StepId.Repository, StepState.Done, $"{git.StdOut.Trim()} · gh signed in · on main");
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        report.Step(StepId.Sync, StepState.Running, "Fetching origin/main");
        await Must("git", ["fetch", "origin", "main"], ct);

        var ahead = int.Parse((await Must("git", ["rev-list", "--count", "origin/main..HEAD"], ct)).StdOut.Trim());
        if (ahead > 0)
            throw new PipelineFailedException($"Local main has {ahead} unpushed commit(s). Push or drop them first so they don't ride along with the post.");

        var behind = int.Parse((await Must("git", ["rev-list", "--count", "HEAD..origin/main"], ct)).StdOut.Trim());
        if (behind > 0)
        {
            report.Detail(StepId.Sync, $"Fast-forwarding {behind} commit(s)");
            var merge = await Run("git", ["merge", "--ff-only", "origin/main"], ct);
            if (!merge.Ok) throw new PipelineFailedException("Could not fast-forward to origin/main. Local changes probably conflict; resolve them in a terminal.");
        }
        report.Step(StepId.Sync, StepState.Done, behind > 0 ? $"Pulled {behind} new commit(s)" : "Up to date with origin/main");
    }

    private void Validate(PostDraft post)
    {
        report.Step(StepId.Validate, StepState.Running, "Checking the post against the importer's rules");
        // Reload after the sync so slug checks see posts published from elsewhere.
        var index = RepoIndex.Load(_repo);
        var result = PostValidator.Validate(post, index);
        foreach (var w in result.Warnings) report.Log(w, LogLevel.Warning);
        foreach (var e in result.Errors) report.Log(e, LogLevel.Error);
        if (!result.Ok)
            throw new PipelineFailedException(result.Errors.Count == 1 ? result.Errors[0] : $"{result.Errors.Count} problems with the post; see the log.");

        report.Step(StepId.Validate, result.Warnings.Count > 0 ? StepState.Warning : StepState.Done,
            $"/blog/{post.Slug}/ · {post.Date:yyyy-MM-dd}" + (result.Warnings.Count > 0 ? $" · {result.Warnings.Count} warning(s)" : ""));
    }

    private List<string> WriteFiles(PostDraft post)
    {
        report.Step(StepId.Write, StepState.Running, "Writing to incoming/blog/");
        var dir = Path.Combine(_repo, "incoming", "blog");
        var targets = new List<(string Target, Action Write)>
        {
            (Path.Combine(dir, post.MarkdownFileName), () => File.WriteAllText(Path.Combine(dir, post.MarkdownFileName), FrontMatter.Render(post))),
            (Path.Combine(dir, post.FeaturedImageFileName), () => File.Copy(post.FeaturedImagePath, Path.Combine(dir, post.FeaturedImageFileName))),
        };
        foreach (var img in post.ExtraImagePaths)
        {
            var target = Path.Combine(dir, Path.GetFileName(img));
            targets.Add((target, () => File.Copy(img, target)));
        }

        var clash = targets.Select(t => t.Target).Where(File.Exists).Select(Path.GetFileName).ToList();
        if (clash.Count > 0)
            throw new PipelineFailedException($"incoming/blog/ already has {string.Join(", ", clash)}. Move or delete it first.");

        var written = new List<string>();
        try
        {
            foreach (var (target, write) in targets)
            {
                write();
                written.Add(target);
                report.Log($"wrote incoming/blog/{Path.GetFileName(target)}");
            }
        }
        catch
        {
            foreach (var f in written) File.Delete(f);
            throw;
        }

        report.Step(StepId.Write, StepState.Done, $"{written.Count} file(s): {string.Join(", ", written.Select(Path.GetFileName))}");
        return written;
    }

    private async Task<string> CommitAndPushAsync(PostDraft post, List<string> written, CancellationToken ct)
    {
        report.Step(StepId.Push, StepState.Running, "Committing");
        var paths = written.Select(p => Path.GetRelativePath(_repo, p).Replace('\\', '/')).ToList();

        try
        {
            await Must("git", ["add", "--", .. paths], ct);
            // Pathspec commit: only these files, even if other changes are staged.
            await Must("git", ["commit", "-m", $"content: add blog post {post.Slug}",
                "-m", "Queued by Blog Post Creator for the Import content workflow.", "--", .. paths], ct);
        }
        catch
        {
            await Run("git", ["reset", "-q", "--", .. paths], CancellationToken.None, quiet: true);
            foreach (var f in written) File.Delete(f);
            throw;
        }

        var sha = (await Must("git", ["rev-parse", "HEAD"], ct)).StdOut.Trim();
        report.Detail(StepId.Push, $"Pushing {sha[..7]} to origin/main");

        var push = await Run("git", ["push", "origin", "HEAD:main"], CancellationToken.None);
        if (!push.Ok)
        {
            // Put the working tree back the way it was so a retry starts clean.
            await Run("git", ["reset", "-q", "--soft", "HEAD~1"], CancellationToken.None, quiet: true);
            await Run("git", ["reset", "-q", "--", .. paths], CancellationToken.None, quiet: true);
            foreach (var f in written) File.Delete(f);
            throw new PipelineFailedException("git push was rejected (see the log). The local commit was undone; sync and try again.");
        }

        report.Step(StepId.Push, StepState.Done, $"Pushed {sha[..7]} to main");
        return sha;
    }

    private async Task<long> WaitForRunAsync(string sha, CancellationToken ct)
    {
        report.Step(StepId.WorkflowQueued, StepState.Running, "Waiting for GitHub to start Import content");
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(3);
        while (DateTime.UtcNow < deadline)
        {
            var list = await Run("gh", ["run", "list", "--workflow", Workflow, "--commit", sha,
                "--json", "databaseId,status,url,event", "--limit", "5"], ct, quiet: true);
            if (list.Ok)
            {
                using var doc = JsonDocument.Parse(list.StdOut);
                var run = doc.RootElement.EnumerateArray()
                    .FirstOrDefault(r => r.GetProperty("event").GetString() == "push");
                if (run.ValueKind == JsonValueKind.Object)
                {
                    var id = run.GetProperty("databaseId").GetInt64();
                    var url = run.GetProperty("url").GetString()!;
                    report.Link("Workflow run", url);
                    report.Step(StepId.WorkflowQueued, StepState.Done, $"Run #{id} {run.GetProperty("status").GetString()}");
                    return id;
                }
            }
            await Task.Delay(PollInterval, ct);
        }
        throw new PipelineFailedException("The Import content workflow didn't start within 3 minutes. Check the Actions tab on GitHub.");
    }

    private async Task<string> FollowRunAsync(long runId, CancellationToken ct)
    {
        report.Step(StepId.ImportBuild, StepState.Running, "Queued");
        var seen = new Dictionary<string, string>();
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(40);
        string url = "";

        while (true)
        {
            if (DateTime.UtcNow > deadline)
                throw new PipelineFailedException("The workflow is still running after 40 minutes. Keep an eye on it on GitHub.");

            var view = await Run("gh", ["run", "view", runId.ToString(), "--json", "status,conclusion,url,jobs"], ct, quiet: true);
            if (!view.Ok)
            {
                await Task.Delay(PollInterval, ct);
                continue;
            }

            using var doc = JsonDocument.Parse(view.StdOut);
            var root = doc.RootElement;
            url = root.GetProperty("url").GetString() ?? url;
            var runStatus = root.GetProperty("status").GetString();
            var runConclusion = root.GetProperty("conclusion").GetString();
            var jobs = root.GetProperty("jobs").EnumerateArray().Select(Job.From).ToList();

            foreach (var job in jobs) LogTransitions(job, seen);

            var import = jobs.FirstOrDefault(j => j.Name == "import");
            var deploy = jobs.Where(j => j.Name.StartsWith("deploy", StringComparison.Ordinal)).ToList();

            // Import job (import script + full validation build + commit).
            if (import is not null && seen.GetValueOrDefault("import:state") != "reported")
            {
                if (import.Status == "completed")
                {
                    seen["import:state"] = "reported";
                    if (import.Conclusion != "success")
                    {
                        await DumpFailedLogAsync(runId, ct);
                        throw new PipelineFailedException($"The import job ended with \"{import.Conclusion}\". The failing step's log is above.");
                    }
                    report.Step(StepId.ImportBuild, StepState.Done, "Imported, built, and committed by the workflow");
                    report.Step(StepId.Deploy, StepState.Running, "Waiting for the deploy job");
                }
                else
                {
                    report.Detail(StepId.ImportBuild, import.CurrentStep ?? import.Status);
                }
            }

            // Deploy jobs come from the reusable deploy.yml ("deploy / build", "deploy / deploy").
            if (seen.GetValueOrDefault("import:state") == "reported")
            {
                var active = deploy.FirstOrDefault(j => j.Status != "completed");
                if (active is not null) report.Detail(StepId.Deploy, $"{active.Name}: {active.CurrentStep ?? active.Status}");
            }

            if (runStatus == "completed")
            {
                if (deploy.Count == 0 || deploy.All(j => j.Conclusion == "skipped"))
                    throw new PipelineFailedException("The import made no changes, so nothing was deployed. Was this exact post already published?");
                if (runConclusion != "success" || deploy.Any(j => j.Conclusion != "success"))
                {
                    await DumpFailedLogAsync(runId, ct);
                    report.Step(StepId.Deploy, StepState.Failed, $"Deploy ended with \"{runConclusion}\"");
                    throw new PipelineFailedException($"The deploy ended with \"{runConclusion}\". The post was imported and committed, but the site wasn't updated.");
                }
                report.Step(StepId.Deploy, StepState.Done, "Published to GitHub Pages");
                return url;
            }

            await Task.Delay(PollInterval, ct);
        }
    }

    private void LogTransitions(Job job, Dictionary<string, string> seen)
    {
        var jobKey = $"job:{job.Name}";
        var jobState = $"{job.Status}/{job.Conclusion}";
        if (seen.GetValueOrDefault(jobKey) != jobState)
        {
            seen[jobKey] = jobState;
            if (job.Status == "in_progress") report.Log($"▶ job {job.Name} started");
            else if (job.Status == "completed")
                report.Log($"{(job.Conclusion == "success" ? "✓" : job.Conclusion == "skipped" ? "–" : "✗")} job {job.Name}: {job.Conclusion}",
                    job.Conclusion is "success" or "skipped" ? LogLevel.Success : LogLevel.Error);
        }
        foreach (var step in job.Steps)
        {
            var key = $"{jobKey}:{step.Number}";
            var state = $"{step.Status}/{step.Conclusion}";
            if (seen.GetValueOrDefault(key) == state) continue;
            seen[key] = state;
            if (step.Status == "completed" && step.Conclusion != "skipped")
                report.Log($"   {(step.Conclusion == "success" ? "✓" : "✗")} {job.Name} › {step.Name}",
                    step.Conclusion == "success" ? LogLevel.Output : LogLevel.Error);
        }
    }

    private async Task DumpFailedLogAsync(long runId, CancellationToken ct)
    {
        var log = await Run("gh", ["run", "view", runId.ToString(), "--log-failed"], ct, quiet: true);
        var lines = log.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.TakeLast(40))
        {
            // gh prefixes each line with "job<TAB>step<TAB>timestamp message".
            var parts = line.Split('\t');
            report.Log(parts[^1], LogLevel.Output);
        }
    }

    private async Task WaitForLiveAsync(string postUrl, CancellationToken ct)
    {
        report.Step(StepId.Live, StepState.Running, postUrl);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.CacheControl = new() { NoCache = true };
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(10);
        var attempt = 0;
        while (DateTime.UtcNow < deadline)
        {
            attempt++;
            HttpStatusCode? status = null;
            try
            {
                using var res = await http.GetAsync($"{postUrl}?bpc={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}", ct);
                status = res.StatusCode;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested) { }

            if (status == HttpStatusCode.OK)
            {
                report.Link("Live post", postUrl);
                report.Step(StepId.Live, StepState.Done, postUrl);
                return;
            }
            report.Detail(StepId.Live, $"Attempt {attempt}: {(status is null ? "no response" : $"HTTP {(int)status}")}; GitHub Pages can take a minute");
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
        throw new PipelineFailedException($"{postUrl} didn't return 200 within 10 minutes of the deploy. The CDN may still be catching up.");
    }

    private async Task PullBackAsync(CancellationToken ct)
    {
        report.Step(StepId.PullBack, StepState.Running, "Fetching the workflow's import commit");
        var fetch = await Run("git", ["fetch", "origin", "main"], ct);
        var merge = fetch.Ok ? await Run("git", ["merge", "--ff-only", "origin/main"], ct) : fetch;
        if (merge.Ok) report.Step(StepId.PullBack, StepState.Done, "Local main now has the imported post");
        else report.Step(StepId.PullBack, StepState.Warning, "Couldn't fast-forward; run git pull yourself");
    }

    // -------------------------------------------------------------- helpers

    private async Task<ProcessResult> Run(string file, string[] args, CancellationToken ct, bool quiet = false)
    {
        if (!quiet) report.Log($"$ {file} {string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}", LogLevel.Command);
        var result = await ProcessRunner.RunAsync(file, args, _repo, ct);
        if (!quiet || !result.Ok)
            foreach (var line in result.Combined.Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(30))
                report.Log(line, result.Ok ? LogLevel.Output : LogLevel.Error);
        return result;
    }

    private async Task<ProcessResult> Must(string file, string[] args, CancellationToken ct)
    {
        var result = await Run(file, args, ct, quiet: true);
        if (!result.Ok) throw new PipelineFailedException($"`{file} {string.Join(' ', args)}` failed: {result.Combined}");
        return result;
    }

    private sealed record JobStep(int Number, string Name, string Status, string? Conclusion);

    private sealed record Job(string Name, string Status, string? Conclusion, IReadOnlyList<JobStep> Steps)
    {
        public string? CurrentStep => Steps.FirstOrDefault(s => s.Status == "in_progress")?.Name;

        public static Job From(JsonElement e) => new(
            e.GetProperty("name").GetString() ?? "",
            e.GetProperty("status").GetString() ?? "",
            NullIfEmpty(e.GetProperty("conclusion").GetString()),
            e.TryGetProperty("steps", out var steps)
                ? steps.EnumerateArray().Select(s => new JobStep(
                    s.GetProperty("number").GetInt32(),
                    s.GetProperty("name").GetString() ?? "",
                    s.GetProperty("status").GetString() ?? "",
                    NullIfEmpty(s.GetProperty("conclusion").GetString()))).ToList()
                : []);

        private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;
    }
}
