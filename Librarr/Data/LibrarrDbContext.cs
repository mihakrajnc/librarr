using Librarr.Model;
using Librarr.Services.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Data;

public class LibrarrDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Book>    Books    { get; set; }
    public DbSet<Author>  Authors  { get; set; }
    public DbSet<LibraryFile>    Files    { get; set; }
    // public DbSet<Torrent> Torrents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Book entity
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.OLID).HasMaxLength(20).IsRequired();
            entity.Property(b => b.Title).HasMaxLength(255).IsRequired();
            entity.Property(b => b.FirstPublishYear).IsRequired();
            entity.Property(b => b.CoverURL).HasMaxLength(255).IsRequired();
            entity.Property(b => b.EBookWanted);
            entity.Property(b => b.AudiobookWanted);

            // Use a shadow property "AuthorId" as the FK for the required Author navigation
            entity.HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey("AuthorId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);;
        });

        // Configure Author entity
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.OLID).HasMaxLength(20).IsRequired();
            entity.Property(a => a.Name).HasMaxLength(127).IsRequired();

            // Unique index on OLID (equivalent to [Index(nameof(OLID), IsUnique = true)])
            entity.HasIndex(a => a.OLID).IsUnique();
        });

        // Configure LibraryFile entity
        modelBuilder.Entity<LibraryFile>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Type).IsRequired();
            entity.Property(f => f.Status).IsRequired();
            entity.Property(f => f.DestinationFiles).HasMaxLength(255);
            entity.Property(f=> f.SourcePath).HasMaxLength(255);
            entity.Property(f=> f.TorrentHash).HasMaxLength(255);
            entity.Property(f=> f.Format).HasMaxLength(4);

            // Use a shadow property "BookId" for the relationship with Book.
            entity.HasOne(f => f.Book)
                .WithMany(b => b.Files)
                .HasForeignKey("BookId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);;
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // For every enum property in your model, store it as a string
        configurationBuilder.Properties<Enum>()
            .HaveConversion<string>();
    }
}