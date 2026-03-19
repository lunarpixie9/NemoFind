using Microsoft.EntityFrameworkCore;
using NemoFind.Core.Interfaces;
using NemoFind.Core.Models;
using NemoFind.Infrastructure.Data;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

// Explicitly alias System.IO to avoid conflict with iTextSharp.Path
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace NemoFind.Infrastructure.Services;

public class IndexerService : IIndexerService
{
    private readonly NemoFindDbContext _context;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "do", "does", "did", "will", "would", "could",
        "should", "may", "might", "shall", "can", "need", "dare", "ought",
        "it", "its", "in", "on", "at", "to", "for", "of", "and", "or", "but",
        "if", "then", "else", "when", "up", "out", "no", "not", "so", "yet",
        "both", "either", "neither", "one", "two", "more", "this", "that",
        "these", "those", "i", "me", "my", "we", "our", "you", "your", "he",
        "she", "him", "her", "they", "them", "his", "their", "what", "which",
        "who", "whom", "from", "as", "into", "through", "with", "about"
    };

    public IndexerService(NemoFindDbContext context)
    {
        _context = context;
    }

    public async Task IndexFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var file = await _context.Files
            .FirstOrDefaultAsync(f => f.Path == filePath, cancellationToken);

        if (file == null) return;

        var text = ExtractText(filePath);
        if (string.IsNullOrWhiteSpace(text)) return;

        var wordFrequencies = Tokenize(text);
        if (wordFrequencies.Count == 0) return;

        // Remove old index entries for this file
        var oldEntries = _context.InvertedIndexes.Where(i => i.FileId == file.Id);
        _context.InvertedIndexes.RemoveRange(oldEntries);

        var totalWords = wordFrequencies.Values.Sum();

        foreach (var (wordText, frequency) in wordFrequencies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var word = await _context.Words
                .FirstOrDefaultAsync(w => w.Text == wordText, cancellationToken);

            if (word == null)
            {
                word = new Word { Text = wordText };
                _context.Words.Add(word);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var tf = (double)frequency / totalWords;

            _context.InvertedIndexes.Add(new InvertedIndex
            {
                WordId = word.Id,
                FileId = file.Id,
                Frequency = frequency,
                TfIdfScore = tf
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  Indexed: {IOPath.GetFileName(filePath)} ({wordFrequencies.Count} unique words)");
    }

    public async Task RemoveFileFromIndexAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var file = await _context.Files
            .FirstOrDefaultAsync(f => f.Path == filePath, cancellationToken);

        if (file == null) return;

        var entries = _context.InvertedIndexes.Where(i => i.FileId == file.Id);
        _context.InvertedIndexes.RemoveRange(entries);

        var hash = _context.FileHashes.Where(h => h.FileId == file.Id);
        _context.FileHashes.RemoveRange(hash);

        _context.Files.Remove(file);

        await _context.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  Removed from index: {IOPath.GetFileName(filePath)}");
    }

    public async Task ReIndexFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"  Re-indexing: {IOPath.GetFileName(filePath)}");
        await IndexFileAsync(filePath, cancellationToken);
    }

    private string ExtractText(string filePath)
    {
        var extension = IOPath.GetExtension(filePath).ToLowerInvariant();

        try
        {
            if (extension == ".pdf")
                return ExtractTextFromPdf(filePath);

            return IOFile.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Could not read {IOPath.GetFileName(filePath)}: {ex.Message}");
            return string.Empty;
        }
    }

    private string ExtractTextFromPdf(string filePath)
    {
        var text = new System.Text.StringBuilder();

        using var reader = new PdfReader(filePath);
        for (int page = 1; page <= reader.NumberOfPages; page++)
        {
            var pageText = PdfTextExtractor.GetTextFromPage(reader, page);
            text.AppendLine(pageText);
        }

        return text.ToString();
    }

    private Dictionary<string, int> Tokenize(string text)
    {
        var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var words = text.Split(new char[]
        {
            ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '"',
            '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '-', '_',
            '@', '#', '$', '%', '^', '&', '*', '+', '=', '|', '<', '>'
        }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawWord in words)
        {
            if (rawWord.Length < 3) continue;
            if (StopWords.Contains(rawWord)) continue;
            if (rawWord.All(char.IsDigit)) continue;

            var normalized = rawWord.ToLowerInvariant();

            if (frequencies.ContainsKey(normalized))
                frequencies[normalized]++;
            else
                frequencies[normalized] = 1;
        }

        return frequencies;
    }
}