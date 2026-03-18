namespace NemoFind.Core.Models;

public class SearchLog
{
    public int Id { get; set; }
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public long SearchDurationMs { get; set; }
    public DateTime SearchedAt { get; set; }
}