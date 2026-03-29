using System.Text.Json.Serialization;
 
namespace NemoFind.Infrastructure.Services.Wikipedia;
 
// Root response from ?action=query&list=search
internal class WikiSearchResponse
{
    [JsonPropertyName("query")]
    public WikiQuery? Query { get; set; }
}
 
internal class WikiQuery
{
    [JsonPropertyName("search")]
    public List<WikiSearchHit> Search { get; set; } = [];
}
 
internal class WikiSearchHit
{
    [JsonPropertyName("pageid")]
    public int PageId { get; set; }
 
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
 
    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;
 
    [JsonPropertyName("wordcount")]
    public int WordCount { get; set; }
 
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
 
// Root response from ?action=query&prop=extracts (single article extract)
internal class WikiExtractResponse
{
    [JsonPropertyName("query")]
    public WikiExtractQuery? Query { get; set; }
}
 
internal class WikiExtractQuery
{
    [JsonPropertyName("pages")]
    public Dictionary<string, WikiPage> Pages { get; set; } = [];
}
 
internal class WikiPage
{
    [JsonPropertyName("pageid")]
    public int PageId { get; set; }
 
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
 
    [JsonPropertyName("extract")]
    public string Extract { get; set; } = string.Empty;
}