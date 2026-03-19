using Microsoft.AspNetCore.Mvc;
using NemoFind.Core.Interfaces;

namespace NemoFind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrawlController : ControllerBase
{
    private readonly ICrawlerService _crawlerService;
    private readonly IIndexerService _indexerService;

    public CrawlController(ICrawlerService crawlerService, IIndexerService indexerService)
    {
        _crawlerService = crawlerService;
        _indexerService = indexerService;
    }

    [HttpPost]
    public async Task<IActionResult> Crawl([FromQuery] string? path = null)
    {
        var rootPath = path ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Run crawl and index in background so API responds immediately
        _ = Task.Run(async () =>
        {
            await _crawlerService.CrawlAsync(rootPath);

            // Index every crawled file
            var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try { await _indexerService.IndexFileAsync(file); }
                catch { /* skip unreadable files */ }
            }
        });

        return Ok(new
        {
            message = "Crawling started in background!",
            path = rootPath
        });
    }
}