using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Web;
using NemoFind.Core.Interfaces;
using NemoFind.Core.Models;
using NemoFind.Infrastructure.Services.Wikipedia;
 
namespace NemoFind.Infrastructure.Services;
 
public class WikipediaSearchService : IWikipediaSearchService
{
    private readonly HttpClient _httpClient;
 
    // Wikipedia's Action API base URL
    private const string ApiBase = "https://en.wikipedia.org/w/api.php";
 
    public WikipediaSearchService(HttpClient httpClient)
    {
        _httpClient = httpClient;
 
        // Wikipedia requires a descriptive User-Agent or it will reject requests
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                "NemoFind/1.0 (Personal file search engine; https://github.com/lunarpixie9/NemoFind)"
            );
        }
    }
 
    public async Task<IEnumerable<WikipediaSearchResult>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<WikipediaSearchResult>();
 
        // Clamp limit to Wikipedia's max for srsearch (500 for bots, 50 for anonymous)
        limit = Math.Clamp(limit, 1, 50);
 
        var encoded = HttpUtility.UrlEncode(query.Trim());
        var url = $"{ApiBase}?action=query&list=search&srsearch={encoded}&srlimit={limit}&srwhat=text&srsort=relevance&srprop=snippet|wordcount|timestamp&format=json&utf8=";
 
        Console.WriteLine($"🐠 NemoFind querying Wikipedia for: {query}");
 
        try
        {
            var response = await _httpClient.GetFromJsonAsync<WikiSearchResponse>(url, cancellationToken);
 
            if (response?.Query?.Search is null || response.Query.Search.Count == 0)
            {
                Console.WriteLine("🐠 Wikipedia returned no results.");
                return Enumerable.Empty<WikipediaSearchResult>();
            }
 
            var results = response.Query.Search.Select(hit => new WikipediaSearchResult
            {
                PageId   = hit.PageId,
                Title    = hit.Title,
                Snippet  = StripHtmlTags(hit.Snippet),  // API returns snippet with <span> highlights
                Url      = BuildArticleUrl(hit.Title),
                WordCount = hit.WordCount,
                Timestamp = hit.Timestamp
            }).ToList();
 
            Console.WriteLine($"🐠 Wikipedia returned {results.Count} result(s) for \"{query}\".");
            return results;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"🐠 Wikipedia request failed: {ex.Message}");
            return Enumerable.Empty<WikipediaSearchResult>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🐠 Unexpected error querying Wikipedia: {ex.Message}");
            return Enumerable.Empty<WikipediaSearchResult>();
        }
    }
 
    public async Task<string?> GetArticleExtractAsync(
        int pageId,
        CancellationToken cancellationToken = default)
    {
        // exintro=true returns only the intro paragraph — enough for summaries
        var url = $"{ApiBase}?action=query&prop=extracts&pageids={pageId}&exintro=true&explaintext=true&format=json&utf8=";
 
        Console.WriteLine($"🐠 Fetching Wikipedia extract for page ID: {pageId}");
 
        try
        {
            var response = await _httpClient.GetFromJsonAsync<WikiExtractResponse>(url, cancellationToken);
            var page = response?.Query?.Pages?.Values.FirstOrDefault();
 
            if (page is null || string.IsNullOrWhiteSpace(page.Extract))
            {
                Console.WriteLine("🐠 No extract found.");
                return null;
            }
 
            return page.Extract.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🐠 Failed to fetch article extract: {ex.Message}");
            return null;
        }
    }
 
    // Wikipedia snippets contain <span class="searchmatch"> tags — strip them for clean text
    private static string StripHtmlTags(string html)
    {
        return Regex.Replace(html, "<.*?>", string.Empty);
    }
 
    // Convert article title to its Wikipedia URL
    // e.g. "Inverted index" -> "https://en.wikipedia.org/wiki/Inverted_index"
    private static string BuildArticleUrl(string title)
    {
        var slug = title.Replace(' ', '_');
        return $"https://en.wikipedia.org/wiki/{HttpUtility.UrlPathEncode(slug)}";
    }
}