using Cheesarr.Model;
using Microsoft.EntityFrameworkCore;

namespace Cheesarr.Data;

public class CheesarrDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<BookEntry> Books { get; set; }
    public DbSet<AuthorEntry> Authors { get; set; }
    public DbSet<FileEntry> Files { get; set; }
    public DbSet<TorrentEntry> Torrents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // modelBuilder.Entity<BookEntry>()
        //     .HasOne(b => b.AudiobookFile)
        //     .WithOne(f => f.Book) // FileEntry must have a BookEntry
        //     .HasForeignKey<FileEntry>(f => f.BookId) // Foreign key lives in FileEntry
        //     .IsRequired(); // FileEntry must always have a BookEntry
        //
        // modelBuilder.Entity<FileEntry>()
        //     .HasOne(f => f.Book)
        //     .WithOne(b => b.AudiobookFile)
        //     .HasForeignKey<BookEntry>(b => b.AudiobookFileId) // Foreign key reference in BookEntry
        //     .IsRequired(false); // AudiobookFile in BookEntry is optional
    }
}