using NemoFind.Core.Interfaces;

namespace NemoFind.API.Services;

public class FileWatcherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<FileSystemWatcher> _watchers = new();

    private static readonly string[] WatchPaths =
    {
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Documents",
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Desktop",
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Downloads",
    };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".cs", ".js", ".py", ".java", ".cpp", ".c",
        ".json", ".xml", ".yaml", ".yml", ".csv", ".html", ".css",
        ".ts", ".go", ".rs", ".swift", ".kt", ".pdf"
    };

    public FileWatcherService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var path in WatchPaths)
        {
            if (!Directory.Exists(path)) continue;

            var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Size
            };

            watcher.Created += OnFileCreated;
            watcher.Changed += OnFileChanged;
            watcher.Deleted += OnFileDeleted;
            watcher.Renamed += OnFileRenamed;

            _watchers.Add(watcher);
            Console.WriteLine($"🐠 Watching: {path}");
        }

        return Task.CompletedTask;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!IsSupportedFile(e.FullPath)) return;
        Console.WriteLine($"  New file detected: {e.Name}");
        _ = HandleFileAsync(e.FullPath);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsSupportedFile(e.FullPath)) return;
        Console.WriteLine($"  File changed: {e.Name}");
        _ = HandleFileAsync(e.FullPath);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!IsSupportedFile(e.FullPath)) return;
        Console.WriteLine($"  File deleted: {e.Name}");
        _ = HandleFileDeletedAsync(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!IsSupportedFile(e.FullPath)) return;
        Console.WriteLine($"  File renamed: {e.OldName} → {e.Name}");
        _ = HandleFileDeletedAsync(e.OldFullPath);
        _ = HandleFileAsync(e.FullPath);
    }

    private async Task HandleFileAsync(string filePath)
    {
        await Task.Delay(500);

        using var scope = _serviceProvider.CreateScope();
        var crawler = scope.ServiceProvider.GetRequiredService<ICrawlerService>();
        var indexer = scope.ServiceProvider.GetRequiredService<IIndexerService>();

        await crawler.CrawlFileAsync(filePath);
        await indexer.IndexFileAsync(filePath);
    }

    private async Task HandleFileDeletedAsync(string filePath)
    {
        using var scope = _serviceProvider.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IIndexerService>();
        await indexer.RemoveFileFromIndexAsync(filePath);
    }

    private bool IsSupportedFile(string filePath)
    {
        return SupportedExtensions.Contains(Path.GetExtension(filePath));
    }

    public override void Dispose()
    {
        foreach (var watcher in _watchers)
            watcher.Dispose();
        base.Dispose();
    }
}