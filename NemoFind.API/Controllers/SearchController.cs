using Microsoft.AspNetCore.Mvc;
using NemoFind.Core.Interfaces;

namespace NemoFind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? type = null,
        [FromQuery] int? sinceDays = null)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Search query cannot be empty.");

        DateTime? since = sinceDays.HasValue
            ? DateTime.UtcNow.AddDays(-sinceDays.Value)
            : null;

        var results = await _searchService.SearchAsync(q, type, since);
        return Ok(results);
    }

    [HttpGet("duplicates")]
    public async Task<IActionResult> GetDuplicates()
    {
        var duplicates = await _searchService.GetDuplicatesAsync();
        return Ok(duplicates);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _searchService.GetStatsAsync();
        return Ok(stats);
    }
}