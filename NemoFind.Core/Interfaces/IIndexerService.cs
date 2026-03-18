namespace NemoFind.Core.Interfaces;

public interface IIndexerService
{
    Task IndexFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task RemoveFileFromIndexAsync(string filePath, CancellationToken cancellationToken = default);
    Task ReIndexFileAsync(string filePath, CancellationToken cancellationToken = default);
}