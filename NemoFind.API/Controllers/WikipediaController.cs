using Microsoft.AspNetCore.Mvc;
using NemoFind.Core.Interfaces;

namespace NemoFind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WikipediaController : ControllerBase
{
    private readonly IWikipediaSearchService _wikipediaService;

    public WikipediaController(IWikipediaSearchService wikipediaService)
    {
        _wikipediaService = wikipediaService;
    }

    // GET /api/Wikipedia/search?q=laptop&limit=10
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");

        var results = await _wikipediaService.SearchAsync(q, limit);
        return Ok(results);
    }

    // GET /api/Wikipedia/extract/12345
    // Returns the intro paragraph of a Wikipedia article — used by UiPath bot for logging
    [HttpGet("extract/{pageId}")]
    public async Task<IActionResult> GetExtract(int pageId)
    {
        var extract = await _wikipediaService.GetArticleExtractAsync(pageId);

        if (extract is null)
            return NotFound("No extract found for this page ID.");

        return Ok(new { pageId, extract });
    }
}