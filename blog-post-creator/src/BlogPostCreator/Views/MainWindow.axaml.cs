using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BlogPostCreator.Services;
using BlogPostCreator.ViewModels;

namespace BlogPostCreator.Views;

public partial class MainWindow : Window, IDialogService
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not MainWindowViewModel vm) return;
        vm.Attach(this);
        // Keep the newest activity in view.
        vm.Log.CollectionChanged += (_, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
                Dispatcher.UIThread.Post(() => LogScroller.ScrollToEnd(), DispatcherPriority.Background);
        };
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        (DataContext as MainWindowViewModel)?.OnClosing();
        base.OnClosing(e);
    }

    public async Task<IReadOnlyList<string>> PickFilesAsync(string title, bool multiple, params FileFilter[] filters)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = multiple,
            FileTypeFilter = filters.Select(f => new FilePickerFileType(f.Name) { Patterns = f.Patterns }).ToList(),
        });
        return files.Select(f => f.TryGetLocalPath()).OfType<string>().ToList();
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
