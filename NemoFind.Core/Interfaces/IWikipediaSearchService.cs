using NemoFind.Core.Models;
 
namespace NemoFind.Core.Interfaces;
 
public interface IWikipediaSearchService
{
    /// <summary>
    /// Search Wikipedia for articles matching the query.
    /// Returns up to <paramref name="limit"/> results.
    /// </summary>
    Task<IEnumerable<WikipediaSearchResult>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);
 
    /// <summary>
    /// Fetch the full plain-text extract of a single Wikipedia article by page ID.
    /// Useful for UiPath bot logging or deeper indexing later.
    /// </summary>
    Task<string?> GetArticleExtractAsync(
        int pageId,
        CancellationToken cancellationToken = default);
}