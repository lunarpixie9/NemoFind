namespace NemoFind.Core.Models;

public class InvertedIndex
{
    public int Id { get; set; }
    public int WordId { get; set; }
    public int FileId { get; set; }
    public int Frequency { get; set; }
    public double TfIdfScore { get; set; }

    // Navigation properties
    public Word Word { get; set; } = null!;
    public IndexedFile File { get; set; } = null!;
}