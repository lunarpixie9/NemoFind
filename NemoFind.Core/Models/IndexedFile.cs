namespace NemoFind.Core.Models;

public class IndexedFile
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime IndexedAt { get; set; }

    // Navigation properties for EF Core
    public ICollection<InvertedIndex> InvertedIndexes { get; set; } = new List<InvertedIndex>();
    public FileHash? FileHash { get; set; }
}