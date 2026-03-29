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

        _context.SearchLogs.Add(new SearchLog
        {
            Query = query,
            SearchedAt = DateTime.UtcNow,
            ResultCount = 0,
            SearchDurationMs = 0
        });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var queryWords = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToList();

        var totalFiles = await _context.Files.CountAsync();
        if (totalFiles == 0) return Enumerable.Empty<SearchResult>();

        var fileScores     = new Dictionary<int, double>();
        var fileMatchCounts = new Dictionary<int, int>();

        // Track how many unique query words each file contains
        // Used to boost files that match ALL words (phrase-like behaviour)
        var fileWordHits = new Dictionary<int, int>();

        foreach (var queryWord in queryWords)
        {
            // FIX 1: also match partial words using StartsWith for short queries
            // e.g. "learn" will also match "learning", "learned"
            var word = await _context.Words
                .FirstOrDefaultAsync(w => w.Text == queryWord);

            if (word == null)
            {
                // Try prefix match for single-word queries
                if (queryWords.Count == 1)
                {
                    word = await _context.Words
                        .Where(w => w.Text.StartsWith(queryWord))
                        .OrderBy(w => w.Text.Length) // prefer shortest (closest) match
                        .FirstOrDefaultAsync();
                }
                if (word == null) continue;
            }

            var indexEntries = await _context.InvertedIndexes
                .Where(i => i.WordId == word.Id)
                .ToListAsync();

            var filesWithWord = indexEntries.Count;

            // FIX 2: use TfIdfScore directly — don't multiply by IDF again.
            // The score is already TF-IDF. We just sum across query words.
            foreach (var entry in indexEntries)
            {
                if (fileScores.ContainsKey(entry.FileId))
                {
                    fileScores[entry.FileId]      += entry.TfIdfScore;
                    fileMatchCounts[entry.FileId] += entry.Frequency;
                    fileWordHits[entry.FileId]    += 1;
                }
                else
                {
                    fileScores[entry.FileId]      = entry.TfIdfScore;
                    fileMatchCounts[entry.FileId] = entry.Frequency;
                    fileWordHits[entry.FileId]    = 1;
                }
            }
        }

        if (fileScores.Count == 0)
            return Enumerable.Empty<SearchResult>();

        var fileIds    = fileScores.Keys.ToList();
        var filesQuery = _context.Files.Where(f => fileIds.Contains(f.Id));

        if (!string.IsNullOrWhiteSpace(fileType))
        {
            var ext = fileType.StartsWith(".") ? fileType : $".{fileType}";
            filesQuery = filesQuery.Where(f => f.Extension == ext.ToLowerInvariant());
        }

        if (since.HasValue)
            filesQuery = filesQuery.Where(f => f.ModifiedAt >= since.Value);

        var files = await filesQuery.ToListAsync();

        var queryLower = query.ToLowerInvariant();

        var results = files.Select(f =>
        {
            var baseScore = fileScores[f.Id];

            // FIX 3: filename boost — file name contains the full query or any query word
            var fileNameLower = f.Name.ToLowerInvariant();
            if (fileNameLower.Contains(queryLower))
                baseScore *= 3.0;   // strong boost — exact phrase in filename
            else if (queryWords.Any(w => fileNameLower.Contains(w)))
                baseScore *= 1.8;   // moderate boost — at least one word in filename

            // FIX 4: all-words bonus — file contains every query word
            // Rewards files that match the full query, not just one word
            if (queryWords.Count > 1 && fileWordHits.TryGetValue(f.Id, out var hits))
            {
                var coverage = (double)hits / queryWords.Count;
                baseScore *= (1.0 + coverage); // up to 2x for full coverage
            }

            return new SearchResult
            {
                FileId         = f.Id,
                FilePath       = f.Path,
                FileName       = f.Name,
                Extension      = f.Extension,
                RelevanceScore = Math.Round(baseScore, 4),
                MatchCount     = fileMatchCounts[f.Id],
                ModifiedAt     = f.ModifiedAt
            };
        })
        .OrderByDescending(r => r.RelevanceScore)
        .Take(50) // cap at 50 results — more than this is noise
        .ToList();

        stopwatch.Stop();

        var log = _context.SearchLogs.Local.LastOrDefault();
        if (log != null)
        {
            log.ResultCount      = results.Count;
            log.SearchDurationMs = stopwatch.ElapsedMilliseconds;
        }

        await _context.SaveChangesAsync();

        return results;
    }

    public async Task<IEnumerable<IndexedFile>> GetDuplicatesAsync()
    {
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
            TotalFiles         = await _context.Files.CountAsync(),
            TotalWords         = await _context.Words.CountAsync(),
            TotalIndexEntries  = await _context.InvertedIndexes.CountAsync(),
            DatabaseSizeBytes  = dbSize,
            LastCrawledAt      = lastFile?.IndexedAt ?? DateTime.MinValue
        };
    }
}