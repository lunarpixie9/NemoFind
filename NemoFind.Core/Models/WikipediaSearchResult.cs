namespace NemoFind.Core.Models;
 
public class WikipediaSearchResult
{
    public int PageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;  // Short excerpt with keyword highlighted
    public string Url { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public DateTime Timestamp { get; set; }
}