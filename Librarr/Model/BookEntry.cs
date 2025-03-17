using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Model;

[Index(nameof(OLID), IsUnique = true)]
public class BookEntry
{
    [Key] public int Id { get; init; }

    [Required, MaxLength(20)]  public required string      OLID             { get; init; }
    [Required, MaxLength(255)] public required string      Title            { get; init; }
    public required                            int         FirstPublishYear { get; init; }
    [Required, MaxLength(255)] public required string      CoverURL         { get; init; }
    [Required]                 public required AuthorEntry Author           { get; init; }

    public bool          EBookWanted      { get; set; } = false;
    public bool          AudiobookWanted  { get; set; } = false;
    public TorrentEntry? EBookTorrent     { get; set; }
    public TorrentEntry? AudiobookTorrent { get; set; }
    public FileEntry?    EBookFile        { get; set; }
    public FileEntry?    AudiobookFile    { get; set; }


    public Status EBookStatus     => GetStatus(EBookTorrent);
    public Status AudiobookStatus => GetStatus(AudiobookTorrent);

    private static Status GetStatus(TorrentEntry? torrent)
    {
        // TODO: Maybe we should also check the file here
        if (torrent == null) return Status.Wanted;
        if (torrent.IsImported) return Status.Imported;

        return torrent.TorrentStatus switch
        {
            TorrentEntry.Status.Missing => Status.Missing,
            TorrentEntry.Status.Downloading => Status.Downloading,
            TorrentEntry.Status.Downloaded => Status.Downloaded,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public enum Status
    {
        Missing, // Torrent was grabbed but it was removed from the client before it could be imported
        Wanted, // Book has been added but release was not found yet

        // Grabbed, // Release was sent to download client
        Downloading, // Download client has reported the book is added and downloading
        Downloaded, // Book has been downloaded but not yet imported
        Imported, // Book has been imported into the library
    }
}