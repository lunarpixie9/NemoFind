namespace NemoFind.Core.Interfaces;

public interface ICrawlerService
{
    Task CrawlAsync(string rootPath, CancellationToken cancellationToken = default);
    Task CrawlFileAsync(string filePath, CancellationToken cancellationToken = default);
}