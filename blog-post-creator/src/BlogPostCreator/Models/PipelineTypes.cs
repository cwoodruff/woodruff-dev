namespace BlogPostCreator.Models;

public enum StepId
{
    Repository,
    Sync,
    Validate,
    Write,
    Push,
    WorkflowQueued,
    ImportBuild,
    Deploy,
    Live,
    PullBack,
}

public enum StepState { Pending, Running, Done, Warning, Failed, Skipped }

public enum LogLevel { Info, Command, Output, Success, Warning, Error }

public interface IPipelineReporter
{
    void Step(StepId id, StepState state, string? detail = null);
    void Detail(StepId id, string detail);
    void Log(string message, LogLevel level = LogLevel.Info);
    void Link(string label, string url);
}

public sealed record PipelineOptions(string RepoRoot, string SiteUrl, bool CheckOnly);

public sealed record PublishOutcome(bool Success, string? PostUrl, string? RunUrl, string Summary);

public sealed class PipelineFailedException(string message) : Exception(message);
