using System.Text.Json;

var API_BASE = "http://localhost:5258/api";

if (args.Length == 0)
{
    PrintHelp();
    return;
}

switch (args[0].ToLower())
{
    case "search":
        await HandleSearch(args);
        break;
    case "crawl":
        await HandleCrawl(args);
        break;
    case "stats":
        await HandleStats();
        break;
    case "duplicates":
        await HandleDuplicates();
        break;
    default:
        PrintHelp();
        break;
}

// ── SEARCH ───────────────────────────────────────────────
async Task HandleSearch(string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: nemofind search \"your query\" [--type pdf] [--since 7]");
        return;
    }

    var query = args[1];
    string? type = null;
    int? sinceDays = null;

    for (int i = 2; i < args.Length; i++)
    {
        if (args[i] == "--type" && i + 1 < args.Length)
            type = args[++i];
        if (args[i] == "--since" && i + 1 < args.Length)
            sinceDays = int.TryParse(args[++i], out var d) ? d : null;
    }

    Console.WriteLine($"\n🐠 NemoFind — Searching for \"{query}\"\n");

    try
    {
        var url = $"{API_BASE}/search?q={Uri.EscapeDataString(query)}";
        if (!string.IsNullOrEmpty(type)) url += $"&type={type}";
        if (sinceDays.HasValue) url += $"&sinceDays={sinceDays}";

        using var client = new HttpClient();
        var response = await client.GetStringAsync(url);
        var results = JsonSerializer.Deserialize<List<SearchResult>>(response,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (results == null || results.Count == 0)
        {
            Console.WriteLine("  No files found. Try different keywords or run: nemofind crawl");
            return;
        }

        Console.WriteLine($"  {results.Count} result(s) found\n");
        Console.WriteLine($"  {"File",-35} {"Matches",-10} {"Score",-10} {"Modified",-12}");
        Console.WriteLine($"  {new string('─', 70)}");

        foreach (var r in results)
        {
            var name = r.FileName.Length > 33 ? r.FileName[..33] + ".." : r.FileName;
            var date = r.ModifiedAt.ToLocalTime().ToString("dd MMM yyyy");
            Console.WriteLine($"  {name,-35} {r.MatchCount,-10} {r.RelevanceScore,-10:F4} {date,-12}");
            Console.WriteLine($"    {r.FilePath}");
            Console.WriteLine();
        }
    }
    catch
    {
        Console.WriteLine("  ⚠️  Could not connect to NemoFind API.");
        Console.WriteLine("  Make sure the API is running: dotnet run --project NemoFind.API");
    }
}

// ── CRAWL ────────────────────────────────────────────────
async Task HandleCrawl(string[] args)
{
    string? path = null;
    for (int i = 1; i < args.Length; i++)
    {
        if (args[i] == "--path" && i + 1 < args.Length)
            path = args[++i];
    }

    var targetPath = path ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    Console.WriteLine($"\n🐠 NemoFind — Starting crawl at: {targetPath}\n");

    try
    {
        using var client = new HttpClient();
        var url = $"{API_BASE}/crawl?path={Uri.EscapeDataString(targetPath)}";
        var response = await client.PostAsync(url, null);

        if (response.IsSuccessStatusCode)
            Console.WriteLine("  ✅ Crawl started in background!");
        else
            Console.WriteLine("  ⚠️  Crawl failed.");
    }
    catch
    {
        Console.WriteLine("  ⚠️  Could not connect to NemoFind API.");
        Console.WriteLine("  Make sure the API is running: dotnet run --project NemoFind.API");
    }
}

// ── STATS ────────────────────────────────────────────────
async Task HandleStats()
{
    Console.WriteLine("\n🐠 NemoFind — Index Statistics\n");

    try
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync($"{API_BASE}/search/stats");
        var stats = JsonSerializer.Deserialize<SearchStats>(response,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (stats == null) return;

        Console.WriteLine($"  📄 Total files indexed : {stats.TotalFiles:N0}");
        Console.WriteLine($"  📝 Unique words        : {stats.TotalWords:N0}");
        Console.WriteLine($"  🔗 Index entries       : {stats.TotalIndexEntries:N0}");
        Console.WriteLine($"  💾 Database size       : {stats.DatabaseSizeBytes / 1024.0:F1} KB");
        Console.WriteLine($"  🕐 Last crawled        : {stats.LastCrawledAt.ToLocalTime():dd MMM yyyy HH:mm}");
        Console.WriteLine();
    }
    catch
    {
        Console.WriteLine("  ⚠️  Could not connect to NemoFind API.");
    }
}

// ── DUPLICATES ───────────────────────────────────────────
async Task HandleDuplicates()
{
    Console.WriteLine("\n🐠 NemoFind — Duplicate Files\n");

    try
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync($"{API_BASE}/search/duplicates");
        var files = JsonSerializer.Deserialize<List<IndexedFile>>(response,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (files == null || files.Count == 0)
        {
            Console.WriteLine("  ✅ No duplicate files found!");
            return;
        }

        Console.WriteLine($"  Found {files.Count} duplicate file(s):\n");
        foreach (var f in files)
            Console.WriteLine($"  📄 {f.Name}\n     {f.Path}\n");
    }
    catch
    {
        Console.WriteLine("  ⚠️  Could not connect to NemoFind API.");
    }
}

// ── HELP ─────────────────────────────────────────────────
void PrintHelp()
{
    Console.WriteLine("""

    🐠 NemoFind — Personal File Search Engine

    Usage:
      nemofind search "query"              Search all indexed files
      nemofind search "query" --type pdf   Search only PDFs
      nemofind search "query" --since 7    Files modified in last 7 days
      nemofind crawl                       Crawl home directory
      nemofind crawl --path ~/Documents    Crawl specific folder
      nemofind stats                       Show index statistics
      nemofind duplicates                  Find duplicate files

    """);
}

// ── MODELS ───────────────────────────────────────────────
public class SearchResult
{
    public int FileId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public int MatchCount { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public class SearchStats
{
    public int TotalFiles { get; set; }
    public int TotalWords { get; set; }
    public int TotalIndexEntries { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public DateTime LastCrawledAt { get; set; }
}

public class IndexedFile
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long Size { get; set; }
}