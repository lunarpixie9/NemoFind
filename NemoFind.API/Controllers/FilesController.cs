using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace NemoFind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    [HttpPost("open")]
    public IActionResult OpenFile([FromBody] FilePathRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest("Path is required.");

        if (!System.IO.File.Exists(request.Path))
            return NotFound("File not found.");

        try
        {
            // macOS — open file with default app
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"\"{request.Path}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            return Ok(new { message = "File opened." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("reveal")]
    public IActionResult RevealInFinder([FromBody] FilePathRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest("Path is required.");

        try
        {
            // macOS — reveal file in Finder
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"-R \"{request.Path}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            return Ok(new { message = "Revealed in Finder." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class FilePathRequest
{
    public string Path { get; set; } = string.Empty;
}