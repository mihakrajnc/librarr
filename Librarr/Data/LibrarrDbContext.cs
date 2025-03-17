using Librarr.Model;
using Librarr.Services.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Data;

public class LibrarrDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<BookEntry>    Books    { get; set; }
    public DbSet<AuthorEntry>  Authors  { get; set; }
    public DbSet<FileEntry>    Files    { get; set; }
    public DbSet<TorrentEntry> Torrents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Torrent entries
        {
            // Shadow property for EBookOfId
            modelBuilder.Entity<TorrentEntry>()
                .Property<int?>("EBookOfId");

            // Relationship for EBookTorrent
            modelBuilder.Entity<BookEntry>()
                .HasOne(b => b.EBookTorrent)
                .WithOne() // no navigation in TorrentEntry
                .HasForeignKey<TorrentEntry>("EBookOfId")
                .OnDelete(DeleteBehavior.Cascade);

            // Shadow property for AudiobookOfId
            modelBuilder.Entity<TorrentEntry>()
                .Property<int?>("AudiobookOfId");

            // Relationship for AudiobookTorrent
            modelBuilder.Entity<BookEntry>()
                .HasOne(b => b.AudiobookTorrent)
                .WithOne()
                .HasForeignKey<TorrentEntry>("AudiobookOfId")
                .OnDelete(DeleteBehavior.Cascade);
        }

        // File entries
        {
            // Shadow property for EBookOfId
            modelBuilder.Entity<FileEntry>()
                .Property<int?>("EBookOfId");

            // Relationship for EBookFile
            modelBuilder.Entity<BookEntry>()
                .HasOne(b => b.EBookFile)
                .WithOne() // no navigation in TorrentEntry
                .HasForeignKey<FileEntry>("EBookOfId")
                .OnDelete(DeleteBehavior.Cascade);

            // Shadow property for AudiobookOfId
            modelBuilder.Entity<FileEntry>()
                .Property<int?>("AudiobookOfId");

            // Relationship for AudiobookFile
            modelBuilder.Entity<BookEntry>()
                .HasOne(b => b.AudiobookFile)
                .WithOne()
                .HasForeignKey<FileEntry>("AudiobookOfId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // For every enum property in your model, store it as a string
        configurationBuilder.Properties<Enum>()
            .HaveConversion<string>();
    }

    // TODO: Should this be here?
    public async Task<BookEntry> AddBook(BookSearchItem bookItem, bool ebook, bool audiobook)
    {
        var authorName = bookItem.AuthorName;
        var authorKey = bookItem.AuthorID;

        var author = Authors.FirstOrDefault(a => a.OLID == authorKey) ?? Authors.Add(new AuthorEntry
        {
            OLID = authorKey,
            Name = authorName
        }).Entity;

        var bookEntry = new BookEntry
        {
            OLID = bookItem.ID,
            Title = bookItem.Title,
            Author = author,
            FirstPublishYear = bookItem.PublishYear,
            EBookWanted = ebook,
            AudiobookWanted = audiobook,
            CoverURL = bookItem.CoverURL
        };

        Books.Add(bookEntry);

        await SaveChangesAsync();

        return bookEntry;
    }
}