using Microsoft.EntityFrameworkCore;
using NemoFind.Core.Models;

namespace NemoFind.Infrastructure.Data;

public class NemoFindDbContext : DbContext
{
    public NemoFindDbContext(DbContextOptions<NemoFindDbContext> options) : base(options) { }

    public DbSet<IndexedFile> Files { get; set; }
    public DbSet<Word> Words { get; set; }
    public DbSet<InvertedIndex> InvertedIndexes { get; set; }
    public DbSet<FileHash> FileHashes { get; set; }
    public DbSet<SearchLog> SearchLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IndexedFile configuration
        modelBuilder.Entity<IndexedFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Path).IsUnique();
            entity.Property(e => e.Path).IsRequired();
            entity.Property(e => e.Name).IsRequired();
        });

        // Word configuration
        modelBuilder.Entity<Word>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Text).IsUnique();
            entity.Property(e => e.Text).IsRequired();
        });

        // InvertedIndex configuration
        modelBuilder.Entity<InvertedIndex>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.WordId, e.FileId }).IsUnique();

            entity.HasOne(e => e.Word)
                  .WithMany(w => w.InvertedIndexes)
                  .HasForeignKey(e => e.WordId);

            entity.HasOne(e => e.File)
                  .WithMany(f => f.InvertedIndexes)
                  .HasForeignKey(e => e.FileId);
        });

        // FileHash configuration
        modelBuilder.Entity<FileHash>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Hash);

            entity.HasOne(e => e.File)
                  .WithOne(f => f.FileHash)
                  .HasForeignKey<FileHash>(e => e.FileId);
        });

        // SearchLog configuration
        modelBuilder.Entity<SearchLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SearchedAt);
        });
    }
}