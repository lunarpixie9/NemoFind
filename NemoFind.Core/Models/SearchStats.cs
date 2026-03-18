namespace NemoFind.Core.Models;

public class SearchStats
{
    public int TotalFiles { get; set; }
    public int TotalWords { get; set; }
    public int TotalIndexEntries { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public DateTime LastCrawledAt { get; set; }
}