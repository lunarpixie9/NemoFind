namespace NemoFind.Core.Models;

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