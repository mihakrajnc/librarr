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
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // For every enum property in your model, store it as a string
        configurationBuilder.Properties<Enum>()
            .HaveConversion<string>();
    }
    
}