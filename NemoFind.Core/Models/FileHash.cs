namespace NemoFind.Core.Models;

public class FileHash
{
    public int Id { get; set; }
    public int FileId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTime ComputedAt { get; set; }

    // Navigation property
    public IndexedFile File { get; set; } = null!;
}