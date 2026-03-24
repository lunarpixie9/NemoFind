using Microsoft.AspNetCore.Mvc;
using NemoFind.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace NemoFind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrawlController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CrawlController(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    [HttpPost]
    public IActionResult Crawl([FromQuery] string? path = null)
    {
        var rootPath = path ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        _ = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine($"🐠 Crawl started: {rootPath}");

                var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".txt", ".md", ".cs", ".js", ".py", ".java", ".cpp", ".c",
                    ".json", ".xml", ".yaml", ".yml", ".csv", ".html", ".css",
                    ".ts", ".go", ".rs", ".swift", ".kt", ".pdf", ".docx"
                };

                var files = Directory.EnumerateFiles(
                    rootPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => extensions.Contains(
                        System.IO.Path.GetExtension(f).ToLowerInvariant()))
                    .Where(f => !f.Contains(".app/"))
                    .Where(f => !f.Contains("node_modules/"))
                    .Where(f => !f.Contains(".git/"))
                    .Where(f => !f.Contains("/bin/"))
                    .Where(f => !f.Contains("/obj/"))
                    .Where(f => !f.Contains("mysql-connector"))
                    .Where(f => !f.Contains("pygame"))
                    .Where(f => !f.Contains("eclipse-"))
                    .ToList();

                Console.WriteLine($"  Found {files.Count} supported files");

                foreach (var file in files)
                {
                    try
                    {
                        // IServiceScopeFactory lives for app lifetime — never disposed
                        using var scope = _scopeFactory.CreateScope();
                        var crawler = scope.ServiceProvider
                            .GetRequiredService<ICrawlerService>();
                        var indexer = scope.ServiceProvider
                            .GetRequiredService<IIndexerService>();

                        await crawler.CrawlFileAsync(file);
                        await indexer.IndexFileAsync(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Skipping {System.IO.Path.GetFileName(file)}: {ex.Message}");
                    }
                }

                Console.WriteLine("🐠 Crawl and index complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Crawl error: {ex.Message}");
            }
        });

        return Ok(new
        {
            message = "Crawling started in background!",
            path = rootPath
        });
    }
}