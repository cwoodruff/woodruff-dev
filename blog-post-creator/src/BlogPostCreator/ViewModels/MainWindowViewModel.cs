using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BlogPostCreator.Models;
using BlogPostCreator.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BlogPostCreator.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IPipelineReporter
{
    private static readonly FileFilter MarkdownFilter = new("Markdown", "*.md", "*.markdown");
    private static readonly FileFilter FeaturedFilter = new("Images", "*.png", "*.jpg", "*.jpeg", "*.webp");
    private static readonly FileFilter ImageFilter = new("Images", "*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif", "*.svg");

    private readonly AppSettings _settings;
    private readonly DispatcherTimer _ticker;
    private readonly DispatcherTimer _autosave;
    private IDialogService? _dialogs;
    private RepoIndex? _index;
    private CancellationTokenSource? _cts;
    private bool _slugEdited;
    private bool _settingSlug;
    private bool _loading;

    public MainWindowViewModel()
    {
        _settings = AppSettings.Load();

        Steps =
        [
            new(StepId.Repository, "Check repository & tools", "git, gh sign-in, on main, drop folder empty"),
            new(StepId.Sync, "Sync with origin/main", "Fetch and fast-forward"),
            new(StepId.Validate, "Validate the post", "Slug, featured image, referenced images"),
            new(StepId.Write, "Write to incoming/blog/", "Markdown + featured image + extras"),
            new(StepId.Push, "Commit & push to main", "Only the post's files are committed"),
            new(StepId.WorkflowQueued, "Start Import content", "GitHub Action picks up the push"),
            new(StepId.ImportBuild, "Import & build", "import-blog.mjs, full Astro build, commit"),
            new(StepId.Deploy, "Deploy to GitHub Pages", "deploy.yml build + deploy"),
            new(StepId.Live, "Verify it's live", "Request /blog/<slug>/ until it answers"),
            new(StepId.PullBack, "Pull the imported post", "Fast-forward local main"),
        ];

        _ticker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _ticker.Tick += (_, _) => { foreach (var s in Steps.Where(s => s.IsRunning)) s.Tick(); };

        _autosave = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _autosave.Tick += (_, _) => { _autosave.Stop(); SaveDraft(); };

        PropertyChanged += OnAnyPropertyChanged;
        RestoreDraft();
        _ = LoadRepoAsync(RepoIndex.Locate(_settings.RepoPath));
    }

    public void Attach(IDialogService dialogs) => _dialogs = dialogs;

    // ------------------------------------------------------------ form state

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _slug = "";
    [ObservableProperty] private DateTime? _date = DateTime.Today;
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _tags = "";
    [ObservableProperty] private bool _draft;
    [ObservableProperty] private string _body = "";
    [ObservableProperty] private int _bodyCaret;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFeaturedImage), nameof(FeaturedImageName))]
    private string? _featuredImagePath;

    [ObservableProperty] private Bitmap? _featuredImage;

    public bool HasFeaturedImage => FeaturedImage is not null;
    public string FeaturedImageName => FeaturedImagePath is null ? "No image chosen" : Path.GetFileName(FeaturedImagePath);

    public ObservableCollection<CategoryOption> Categories { get; } = [];
    public ObservableCollection<AttachedImage> ExtraImages { get; } = [];
    public ObservableCollection<Issue> Issues { get; } = [];

    public string PostUrlPreview => $"{_settings.SiteUrl.Replace("https://", "")}/blog/{(string.IsNullOrEmpty(Slug) ? "<slug>" : Slug)}/";

    public string BodyStats
    {
        get
        {
            var words = Regex.Matches(Body, @"[\p{L}\p{N}][\p{L}\p{N}'’\-]*").Count;
            var minutes = Math.Max(1, (int)Math.Round(words / 225.0));
            return words == 0 ? "Empty" : $"{words.ToString("N0", CultureInfo.CurrentCulture)} words · {minutes} min read";
        }
    }

    public bool HasErrors => Issues.Any(i => i.IsError);
    public bool HasIssues => Issues.Count > 0;

    // ------------------------------------------------------------- repo state

    [ObservableProperty] private string _repoPath = "Locating the woodruff-dev repo…";
    [ObservableProperty] private string _repoSummary = "";
    [ObservableProperty] private bool _repoReady;

    // --------------------------------------------------------- pipeline state

    public ObservableCollection<StepViewModel> Steps { get; }
    public ObservableCollection<LogEntry> Log { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(PublishCommand), nameof(CheckCommand), nameof(CancelCommand), nameof(NewPostCommand))]
    private bool _isRunning;

    public bool IsIdle => !IsRunning;

    [ObservableProperty] private bool _isConfirmingPublish;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _headline = "Ready when you are";
    [ObservableProperty] private string _subhead = "Fill in the post, then publish. Every step reports here.";
    [ObservableProperty] private string? _runUrl;
    [ObservableProperty] private string? _livePostUrl;
    [ObservableProperty] private string _windowTitle = "Blog Post Creator · woodruff.dev";

    public bool HasRunUrl => RunUrl is not null;
    public bool HasLivePostUrl => LivePostUrl is not null;

    partial void OnRunUrlChanged(string? value) => OnPropertyChanged(nameof(HasRunUrl));
    partial void OnLivePostUrlChanged(string? value) => OnPropertyChanged(nameof(HasLivePostUrl));

    // ------------------------------------------------------- change tracking

    partial void OnTitleChanged(string value)
    {
        if (!_slugEdited) SetSlugQuietly(Services.Slug.From(value));
    }

    partial void OnSlugChanged(string value)
    {
        if (!_settingSlug) _slugEdited = !string.IsNullOrEmpty(value) && value != Services.Slug.From(Title);
        OnPropertyChanged(nameof(PostUrlPreview));
    }

    partial void OnBodyChanged(string value)
    {
        OnPropertyChanged(nameof(BodyStats));
        RefreshReferencedFlags();
    }

    partial void OnFeaturedImagePathChanged(string? value)
    {
        FeaturedImage?.Dispose();
        FeaturedImage = null;
        if (value is not null && File.Exists(value) && !value.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var stream = File.OpenRead(value);
                FeaturedImage = Bitmap.DecodeToWidth(stream, 900);
            }
            catch
            {
                // .webp or a damaged file: keep going without a preview.
            }
        }
        OnPropertyChanged(nameof(HasFeaturedImage));
    }

    private void SetSlugQuietly(string slug)
    {
        _settingSlug = true;
        Slug = slug;
        _settingSlug = false;
    }

    private void OnAnyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_loading) return;
        if (e.PropertyName is nameof(Title) or nameof(Slug) or nameof(Date) or nameof(Description) or nameof(Tags)
            or nameof(Draft) or nameof(Body) or nameof(FeaturedImagePath))
        {
            Revalidate();
            _autosave.Stop();
            _autosave.Start();
        }
    }

    private void RefreshReferencedFlags()
    {
        var names = MarkdownImages.LocalRefs(Body).Select(Path.GetFileName).ToHashSet();
        foreach (var img in ExtraImages) img.IsReferenced = names.Contains(img.Name);
    }

    private PostDraft BuildDraft() => new()
    {
        Title = Title.Trim(),
        Slug = Slug.Trim(),
        Date = DateOnly.FromDateTime(Date ?? DateTime.Today),
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        Categories = Categories.Where(c => c.IsSelected).Select(c => c.Name).ToList(),
        Tags = Tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToList(),
        Draft = Draft,
        Body = Body,
        FeaturedImagePath = FeaturedImagePath ?? "",
        ExtraImagePaths = ExtraImages.Select(i => i.Path).ToList(),
    };

    private void Revalidate()
    {
        Issues.Clear();
        if (_index is null)
        {
            Issues.Add(new Issue("The site repo isn't loaded yet.", true));
        }
        else
        {
            var result = PostValidator.Validate(BuildDraft(), _index);
            foreach (var e in result.Errors) Issues.Add(new Issue(e, true));
            foreach (var w in result.Warnings) Issues.Add(new Issue(w, false));
        }
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(HasIssues));
        PublishCommand.NotifyCanExecuteChanged();
        CheckCommand.NotifyCanExecuteChanged();
    }

    // ------------------------------------------------------------------ repo

    private async Task LoadRepoAsync(string? path)
    {
        if (path is null || !RepoIndex.LooksLikeSiteRepo(path))
        {
            RepoReady = false;
            RepoPath = "Site repo not found";
            RepoSummary = "Choose the woodruff-dev folder";
            Revalidate();
            return;
        }

        RepoPath = path;
        RepoSummary = "Indexing posts…";
        try
        {
            _index = await Task.Run(() => RepoIndex.Load(path));
        }
        catch (Exception ex)
        {
            RepoSummary = $"Couldn't read posts: {ex.Message}";
            return;
        }

        _settings.RepoPath = path;
        _settings.Save();

        var selected = Categories.Where(c => c.IsSelected).Select(c => c.Name)
            .Concat(_pendingCategories).ToHashSet();
        _pendingCategories.Clear();
        Categories.Clear();
        foreach (var (name, count) in _index.Categories)
        {
            var option = new CategoryOption(name, count) { IsSelected = selected.Contains(name) };
            option.PropertyChanged += (_, _) => { Revalidate(); _autosave.Stop(); _autosave.Start(); };
            Categories.Add(option);
        }

        RepoReady = true;
        RepoSummary = $"{_index.Posts.Count:N0} posts · {_index.Categories.Count} categories";
        Revalidate();
    }

    private readonly List<string> _pendingCategories = [];

    [RelayCommand]
    private async Task ChooseRepo()
    {
        if (_dialogs is null) return;
        var folder = await _dialogs.PickFolderAsync("Choose the woodruff-dev repository");
        if (folder is null) return;
        var root = RepoIndex.Locate(folder);
        if (root is null)
        {
            AddLog($"{folder} doesn't look like the woodruff-dev repo (no incoming/blog/README.md and site/).", LogLevel.Error);
            return;
        }
        await LoadRepoAsync(root);
    }

    // ------------------------------------------------------------- authoring

    [RelayCommand]
    private async Task OpenMarkdown()
    {
        if (_dialogs is null) return;
        var file = (await _dialogs.PickFilesAsync("Open a Markdown post", false, MarkdownFilter)).FirstOrDefault();
        if (file is null) return;

        ParsedMarkdown parsed;
        try
        {
            parsed = FrontMatter.Parse(await File.ReadAllTextAsync(file));
        }
        catch (Exception ex)
        {
            AddLog($"Couldn't read {Path.GetFileName(file)}: {ex.Message}", LogLevel.Error);
            return;
        }

        var fm = parsed.Data;
        string? Get(string key) => fm.TryGetValue(key, out var v) ? v?.ToString() : null;
        var body = parsed.Body.TrimStart();
        var title = Get("title");
        if (title is null)
        {
            // Same rule as the importer: a leading H1 supplies the title and is dropped.
            var h1 = Regex.Match(body, @"^#\s+(.+?)\s*$", RegexOptions.Multiline);
            if (h1.Success && h1.Index == 0)
            {
                title = h1.Groups[1].Value;
                body = body[h1.Length..].TrimStart('\r', '\n');
            }
        }
        var dir = Path.GetDirectoryName(file)!;
        var baseName = Path.GetFileNameWithoutExtension(file);

        _loading = true;
        try
        {
            Title = title ?? Regex.Replace(baseName, "[-_]+", " ");
            _slugEdited = false;
            if (Get("slug") is { } slug) Slug = Services.Slug.From(slug);
            else SetSlugQuietly(Services.Slug.From(Title));
            Date = DateTime.TryParse(Get("date"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.Date : DateTime.Today;
            Description = Get("description") ?? "";
            var cats = fm.TryGetValue("categories", out var c) ? FrontMatter.AsList(c) : [];
            foreach (var option in Categories) option.IsSelected = cats.Contains(option.Name);
            Tags = fm.TryGetValue("tags", out var t) ? string.Join(", ", FrontMatter.AsList(t)) : "";
            Draft = string.Equals(Get("draft"), "true", StringComparison.OrdinalIgnoreCase);
            Body = body;

            FeaturedImagePath = Get("coverImage") is { } cover && File.Exists(Path.Combine(dir, cover))
                ? Path.GetFullPath(Path.Combine(dir, cover))
                : new[] { ".png", ".jpg", ".jpeg", ".webp" }.Select(ext => Path.Combine(dir, baseName + ext)).FirstOrDefault(File.Exists);

            ExtraImages.Clear();
            foreach (var r in MarkdownImages.LocalRefs(body))
            {
                var candidates = new[] { Path.Combine(dir, r), Path.Combine(dir, Path.GetFileName(r)), Path.Combine(dir, "images", Path.GetFileName(r)) };
                var found = candidates.FirstOrDefault(File.Exists);
                if (found is not null && ExtraImages.All(i => i.Name != Path.GetFileName(found)))
                    ExtraImages.Add(new AttachedImage(Path.GetFullPath(found)));
            }
        }
        finally
        {
            _loading = false;
        }
        RefreshReferencedFlags();
        Revalidate();
        SaveDraft();
        AddLog($"Loaded {Path.GetFileName(file)}" + (ExtraImages.Count > 0 ? $" with {ExtraImages.Count} referenced image(s)" : ""), LogLevel.Success);
    }

    [RelayCommand]
    private async Task ChooseFeaturedImage()
    {
        if (_dialogs is null) return;
        var file = (await _dialogs.PickFilesAsync("Choose the featured image", false, FeaturedFilter)).FirstOrDefault();
        if (file is not null) FeaturedImagePath = file;
    }

    [RelayCommand]
    private async Task AddImages()
    {
        if (_dialogs is null) return;
        foreach (var file in await _dialogs.PickFilesAsync("Add images used in the post", true, ImageFilter))
            if (ExtraImages.All(i => i.Path != file)) ExtraImages.Add(new AttachedImage(file));
        RefreshReferencedFlags();
        Revalidate();
        SaveDraft();
    }

    [RelayCommand]
    private void RemoveImage(AttachedImage image)
    {
        ExtraImages.Remove(image);
        Revalidate();
        SaveDraft();
    }

    [RelayCommand]
    private void InsertImage(AttachedImage image)
    {
        var alt = Regex.Replace(Path.GetFileNameWithoutExtension(image.Name), "[-_]+", " ");
        var snippet = $"![{alt}]({image.Name})";
        var at = Math.Clamp(BodyCaret, 0, Body.Length);
        var before = at > 0 && Body[at - 1] != '\n' ? "\n\n" : "";
        Body = Body[..at] + before + snippet + "\n\n" + Body[at..];
        BodyCaret = at + before.Length + snippet.Length + 2;
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void NewPost()
    {
        _loading = true;
        Title = "";
        _slugEdited = false;
        SetSlugQuietly("");
        Date = DateTime.Today;
        Description = "";
        Tags = "";
        Draft = false;
        Body = "";
        FeaturedImagePath = null;
        ExtraImages.Clear();
        foreach (var c in Categories) c.IsSelected = false;
        _loading = false;
        ResetPipeline();
        Headline = "Ready when you are";
        Subhead = "Fill in the post, then publish. Every step reports here.";
        SavedDraft.Clear();
        Revalidate();
    }

    // ---------------------------------------------------------------- publish

    private bool CanStart() => !IsRunning && RepoReady && !HasErrors;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task Check() => StartAsync(checkOnly: true);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Publish() => IsConfirmingPublish = true;

    [RelayCommand]
    private void CancelConfirm() => IsConfirmingPublish = false;

    [RelayCommand]
    private Task ConfirmPublish()
    {
        IsConfirmingPublish = false;
        return StartAsync(checkOnly: false);
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private void OpenUrl(string? url)
    {
        if (!string.IsNullOrEmpty(url)) DesktopNotifier.OpenUrl(url);
    }

    [RelayCommand]
    private void ClearLog() => Log.Clear();

    private async Task StartAsync(bool checkOnly)
    {
        if (_index is null) return;
        SaveDraft();
        ResetPipeline();
        IsRunning = true;
        _ticker.Start();
        _cts = new CancellationTokenSource();
        var post = BuildDraft();
        Headline = checkOnly ? "Checking…" : "Publishing…";
        Subhead = checkOnly ? "Running the local checks only. Nothing is pushed." : $"\"{post.Title}\"";
        AddLog(checkOnly ? $"Checking \"{post.Title}\"" : $"Publishing \"{post.Title}\" as /blog/{post.Slug}/", LogLevel.Info);

        var options = new PipelineOptions(_index.RepoRoot, _settings.SiteUrl, checkOnly);
        var outcome = await Task.Run(() => new PublishPipeline(this).RunAsync(post, options, _cts.Token));

        _ticker.Stop();
        foreach (var s in Steps) s.Tick();
        IsRunning = false;
        _cts.Dispose();
        _cts = null;

        Headline = outcome.Success ? (checkOnly ? "Ready to publish" : post.Draft ? "Imported as a draft" : "Live on woodruff.dev") : "Stopped";
        Subhead = outcome.Summary;
        Progress = outcome.Success ? 100 : Progress;
        WindowTitle = outcome.Success ? $"✓ {Headline} · Blog Post Creator" : "✗ Stopped · Blog Post Creator";
        LivePostUrl = outcome.PostUrl;

        if (!checkOnly)
        {
            DesktopNotifier.Notify(outcome.Success ? "Blog post published" : "Blog post stopped", outcome.Summary);
            if (outcome.Success)
            {
                SavedDraft.Clear();
                await LoadRepoAsync(_index.RepoRoot);
            }
        }
    }

    private void ResetPipeline()
    {
        foreach (var s in Steps) s.Reset();
        Progress = 0;
        RunUrl = null;
        LivePostUrl = null;
        WindowTitle = "Blog Post Creator · woodruff.dev";
    }

    // ------------------------------------------------- IPipelineReporter (any thread)

    void IPipelineReporter.Step(StepId id, StepState state, string? detail) => Dispatcher.UIThread.Post(() =>
    {
        var step = Steps[(int)id];
        step.State = state;
        if (detail is not null) step.Detail = detail;
        var finished = Steps.Count(s => s.IsFinished);
        Progress = Math.Round(100.0 * finished / Steps.Count);
        if (state == StepState.Running)
        {
            Headline = $"{step.Title}…";
            WindowTitle = $"{finished}/{Steps.Count} · {step.Title} · Blog Post Creator";
        }
    });

    void IPipelineReporter.Detail(StepId id, string detail) => Dispatcher.UIThread.Post(() => Steps[(int)id].Detail = detail);

    void IPipelineReporter.Log(string message, LogLevel level) => Dispatcher.UIThread.Post(() => AddLog(message, level));

    void IPipelineReporter.Link(string label, string url) => Dispatcher.UIThread.Post(() =>
    {
        if (label == "Workflow run") RunUrl = url;
        else LivePostUrl = url;
        AddLog($"{label}: {url}", LogLevel.Info);
    });

    private void AddLog(string message, LogLevel level)
    {
        Log.Add(new LogEntry(DateTime.Now.ToString("HH:mm:ss"), message, level));
        while (Log.Count > 2000) Log.RemoveAt(0);
    }

    // ------------------------------------------------------------- persistence

    private void SaveDraft()
    {
        if (string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Body)) return;
        try
        {
            new SavedDraft
            {
                Title = Title,
                Slug = Slug,
                SlugEdited = _slugEdited,
                Date = Date,
                Description = Description,
                Categories = Categories.Where(c => c.IsSelected).Select(c => c.Name).ToList(),
                Tags = Tags,
                Draft = Draft,
                Body = Body,
                FeaturedImagePath = FeaturedImagePath,
                ExtraImagePaths = ExtraImages.Select(i => i.Path).ToList(),
            }.Save();
        }
        catch
        {
            // Autosave is a convenience; never interrupt writing for it.
        }
    }

    private void RestoreDraft()
    {
        var saved = SavedDraft.Load();
        if (saved is null) return;
        _loading = true;
        Title = saved.Title;
        SetSlugQuietly(saved.Slug);
        _slugEdited = saved.SlugEdited;
        Date = saved.Date ?? DateTime.Today;
        Description = saved.Description;
        Tags = saved.Tags;
        Draft = saved.Draft;
        Body = saved.Body;
        FeaturedImagePath = saved.FeaturedImagePath;
        foreach (var p in saved.ExtraImagePaths) ExtraImages.Add(new AttachedImage(p));
        _pendingCategories.AddRange(saved.Categories);
        _loading = false;
        RefreshReferencedFlags();
        AddLog("Restored your unpublished draft.", LogLevel.Info);
    }

    public void OnClosing() => SaveDraft();
}
