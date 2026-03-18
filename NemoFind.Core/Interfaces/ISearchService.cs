using NemoFind.Core.Models;

namespace NemoFind.Core.Interfaces;

public interface ISearchService
{
    Task<IEnumerable<SearchResult>> SearchAsync(string query, string? fileType = null, DateTime? since = null);
    Task<IEnumerable<IndexedFile>> GetDuplicatesAsync();
    Task<SearchStats> GetStatsAsync();
}