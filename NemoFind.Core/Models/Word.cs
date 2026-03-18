namespace NemoFind.Core.Models;

public class Word
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;

    // Navigation property
    public ICollection<InvertedIndex> InvertedIndexes { get; set; } = new List<InvertedIndex>();
}