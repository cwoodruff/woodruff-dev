using BlogPostCreator.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BlogPostCreator.ViewModels;

public sealed partial class StepViewModel(StepId id, string title, string hint) : ObservableObject
{
    public StepId Id { get; } = id;
    public string Title { get; } = title;
    public string Hint { get; } = hint;
    public int Number => (int)Id + 1;

    private DateTime? _startedAt;
    private DateTime? _endedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending), nameof(IsRunning), nameof(IsDone), nameof(IsWarning), nameof(IsFailed), nameof(IsSkipped), nameof(IsFinished))]
    private StepState _state = StepState.Pending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayDetail))]
    private string? _detail;

    [ObservableProperty]
    private string _elapsed = "";

    public bool IsPending => State == StepState.Pending;
    public bool IsRunning => State == StepState.Running;
    public bool IsDone => State == StepState.Done;
    public bool IsWarning => State == StepState.Warning;
    public bool IsFailed => State == StepState.Failed;
    public bool IsSkipped => State == StepState.Skipped;
    public bool IsFinished => State is StepState.Done or StepState.Warning or StepState.Skipped;

    public string DisplayDetail => string.IsNullOrEmpty(Detail) ? Hint : Detail;

    partial void OnStateChanged(StepState value)
    {
        if (value == StepState.Running) { _startedAt ??= DateTime.Now; _endedAt = null; }
        else if (value != StepState.Pending && _startedAt is not null) _endedAt ??= DateTime.Now;
        Tick();
    }

    public void Tick()
    {
        if (_startedAt is null) { Elapsed = ""; return; }
        var span = (_endedAt ?? DateTime.Now) - _startedAt.Value;
        Elapsed = span.TotalMinutes >= 1 ? $"{(int)span.TotalMinutes}m {span.Seconds:00}s" : $"{span.Seconds}s";
    }

    public void Reset()
    {
        _startedAt = null;
        _endedAt = null;
        State = StepState.Pending;
        Detail = null;
        Elapsed = "";
    }
}

public sealed record LogEntry(string Time, string Text, LogLevel Level)
{
    public bool IsCommand => Level == LogLevel.Command;
    public bool IsOutput => Level == LogLevel.Output;
    public bool IsSuccess => Level == LogLevel.Success;
    public bool IsWarning => Level == LogLevel.Warning;
    public bool IsError => Level == LogLevel.Error;
}

public sealed record Issue(string Text, bool IsError);

public sealed partial class CategoryOption(string name, int count) : ObservableObject
{
    public string Name { get; } = name;
    public int Count { get; } = count;
    public string Display => Count > 0 ? $"{Name}  {Count}" : Name;

    [ObservableProperty]
    private bool _isSelected;
}

public sealed partial class AttachedImage(string path) : ObservableObject
{
    public string Path { get; } = path;
    public string Name => System.IO.Path.GetFileName(Path);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnreferenced))]
    private bool _isReferenced;

    public bool IsUnreferenced => !IsReferenced;
}
