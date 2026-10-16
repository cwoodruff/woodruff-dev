namespace BlogPostCreator.Services;

public sealed record FileFilter(string Name, params string[] Patterns);

public interface IDialogService
{
    Task<IReadOnlyList<string>> PickFilesAsync(string title, bool multiple, params FileFilter[] filters);
    Task<string?> PickFolderAsync(string title);
}
