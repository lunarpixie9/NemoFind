using Microsoft.EntityFrameworkCore;
using NemoFind.Core.Interfaces;
using NemoFind.Core.Models;
using NemoFind.Infrastructure.Data;

namespace NemoFind.Infrastructure.Services;

public class CrawlerService : ICrawlerService
{
    private readonly NemoFindDbContext _context;

    // File extensions NemoFind can read and index
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".cs", ".js", ".py", ".java", ".cpp", ".c",
        ".json", ".xml", ".yaml", ".yml", ".csv", ".html", ".css",
        ".ts", ".go", ".rs", ".swift", ".kt", ".pdf"
    };

    public CrawlerService(NemoFindDbContext context)
    {
        _context = context;
    }

    // Crawl an entire directory recursively
    public async Task CrawlAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"🐠 NemoFind starting crawl at: {rootPath}");

        if (!Directory.Exists(rootPath))
        {
            Console.WriteLine($"Directory not found: {rootPath}");
            return;
        }

        var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)));

        foreach (var filePath in files)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await CrawlFileAsync(filePath, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipping {filePath}: {ex.Message}");
            }
        }

        Console.WriteLine("🐠 Crawl complete!");
    }

    // Crawl a single file
    public async Task CrawlFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists) return;

        // Check if file already exists in DB
        var existingFile = await _context.Files
            .FirstOrDefaultAsync(f => f.Path == filePath, cancellationToken);

        if (existingFile != null)
        {
            // Only re-index if file was modified since last index
            if (fileInfo.LastWriteTimeUtc <= existingFile.ModifiedAt) return;

            // Update existing record
            existingFile.ModifiedAt = fileInfo.LastWriteTimeUtc;
            existingFile.Size = fileInfo.Length;
            existingFile.IndexedAt = DateTime.UtcNow;
            _context.Files.Update(existingFile);
        }
        else
        {
            // Add new file record
            var indexedFile = new IndexedFile
            {
                Path = filePath,
                Name = fileInfo.Name,
                Extension = fileInfo.Extension.ToLowerInvariant(),
                Size = fileInfo.Length,
                CreatedAt = fileInfo.CreationTimeUtc,
                ModifiedAt = fileInfo.LastWriteTimeUtc,
                IndexedAt = DateTime.UtcNow
            };

            _context.Files.Add(indexedFile);
        }

        await _context.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  Crawled: {fileInfo.Name}");
    }
}