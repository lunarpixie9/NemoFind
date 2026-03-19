using Microsoft.EntityFrameworkCore;
using NemoFind.Core.Interfaces;
using NemoFind.Core.Models;
using NemoFind.Infrastructure.Data;

namespace NemoFind.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly NemoFindDbContext _context;

    public SearchService(NemoFindDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(
        string query,
        string? fileType = null,
        DateTime? since = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<SearchResult>();

        // Log the search
        _context.SearchLogs.Add(new SearchLog
        {
            Query = query,
            SearchedAt = DateTime.UtcNow,
            ResultCount = 0,
            SearchDurationMs = 0
        });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Split query into individual words (handles multi-word search)
        var queryWords = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .ToList();

        // Get total number of files for IDF calculation
        var totalFiles = await _context.Files.CountAsync();
        if (totalFiles == 0) return Enumerable.Empty<SearchResult>();

        // Find all files that contain ANY of the query words
        var fileScores = new Dictionary<int, double>();
        var fileMatchCounts = new Dictionary<int, int>();

        foreach (var queryWord in queryWords)
        {
            // Find this word in the index
            var word = await _context.Words
                .FirstOrDefaultAsync(w => w.Text == queryWord);

            if (word == null) continue;

            // Get all files containing this word
            var indexEntries = await _context.InvertedIndexes
                .Where(i => i.WordId == word.Id)
                .ToListAsync();

            // Calculate IDF — words in fewer files are more significant
            var filesWithWord = indexEntries.Count;
            var idf = Math.Log((double)totalFiles / (filesWithWord + 1));

            foreach (var entry in indexEntries)
            {
                var tfidf = entry.TfIdfScore * idf;

                if (fileScores.ContainsKey(entry.FileId))
                {
                    fileScores[entry.FileId] += tfidf;
                    fileMatchCounts[entry.FileId] += entry.Frequency;
                }
                else
                {
                    fileScores[entry.FileId] = tfidf;
                    fileMatchCounts[entry.FileId] = entry.Frequency;
                }
            }
        }

        if (fileScores.Count == 0)
            return Enumerable.Empty<SearchResult>();

        // Get the actual file records for all matching files
        var fileIds = fileScores.Keys.ToList();
        var filesQuery = _context.Files.Where(f => fileIds.Contains(f.Id));

        // Apply file type filter if specified
        if (!string.IsNullOrWhiteSpace(fileType))
        {
            var ext = fileType.StartsWith(".") ? fileType : $".{fileType}";
            filesQuery = filesQuery.Where(f => f.Extension == ext.ToLowerInvariant());
        }

        // Apply date filter if specified
        if (since.HasValue)
        {
            filesQuery = filesQuery.Where(f => f.ModifiedAt >= since.Value);
        }

        var files = await filesQuery.ToListAsync();

        // Build ranked results
        var results = files
            .Select(f => new SearchResult
            {
                FileId = f.Id,
                FilePath = f.Path,
                FileName = f.Name,
                Extension = f.Extension,
                RelevanceScore = Math.Round(fileScores[f.Id], 4),
                MatchCount = fileMatchCounts[f.Id],
                ModifiedAt = f.ModifiedAt
            })
            .OrderByDescending(r => r.RelevanceScore)
            .ToList();

        stopwatch.Stop();

        // Update search log with actual results
        var log = _context.SearchLogs.Local.LastOrDefault();
        if (log != null)
        {
            log.ResultCount = results.Count;
            log.SearchDurationMs = stopwatch.ElapsedMilliseconds;
        }

        await _context.SaveChangesAsync();

        return results;
    }

    public async Task<IEnumerable<IndexedFile>> GetDuplicatesAsync()
    {
        // Find files that share the same hash — these are duplicates
        var duplicateHashes = await _context.FileHashes
            .GroupBy(h => h.Hash)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync();

        var duplicateFileIds = await _context.FileHashes
            .Where(h => duplicateHashes.Contains(h.Hash))
            .Select(h => h.FileId)
            .ToListAsync();

        return await _context.Files
            .Where(f => duplicateFileIds.Contains(f.Id))
            .ToListAsync();
    }

    public async Task<SearchStats> GetStatsAsync()
    {
        var dbPath = "nemofind.db";
        long dbSize = 0;

        if (System.IO.File.Exists(dbPath))
            dbSize = new System.IO.FileInfo(dbPath).Length;

        var lastFile = await _context.Files
            .OrderByDescending(f => f.IndexedAt)
            .FirstOrDefaultAsync();

        return new SearchStats
        {
            TotalFiles = await _context.Files.CountAsync(),
            TotalWords = await _context.Words.CountAsync(),
            TotalIndexEntries = await _context.InvertedIndexes.CountAsync(),
            DatabaseSizeBytes = dbSize,
            LastCrawledAt = lastFile?.IndexedAt ?? DateTime.MinValue
        };
    }
}