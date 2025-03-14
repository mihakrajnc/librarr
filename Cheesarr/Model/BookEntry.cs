using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Cheesarr.Model;

[Index(nameof(OLID), IsUnique = true)]
public class BookEntry
{
    [Key]                      public int    Id               { get; init; }
    [Required, MaxLength(20)]  public string OLID             { get; init; } = string.Empty;
    [Required, MaxLength(20)]  public string CoverEditionKey  { get; init; } = string.Empty;
    [Required, MaxLength(255)] public string Title            { get; init; } = string.Empty;
    public                            int    FirstPublishYear { get; init; } = -1;

    public virtual required AuthorEntry   Author           { get; init; }
    public                  TorrentEntry? EBookTorrent     { get; set; }
    public                  TorrentEntry? AudiobookTorrent { get; set; }
    public                  FileEntry?    EBookFile        { get; set; }
    public                  FileEntry?    AudiobookFile    { get; set; }


    // public                                        int         AuthorId { get; set; }
    // [ForeignKey(nameof(AuthorId))] public virtual AuthorEntry Author   { get; set; }

    public BookEntryType WantedTypes     { get; set; } = BookEntryType.None;
    public Status        EBookStatus     { get; set; } = Status.Missing;
    public Status        AudiobookStatus { get; set; } = Status.Missing;
}

// public enum GrabType
// {
//     EBook,
//     Audiobook,
//     Both,
// }

[Flags]
public enum BookEntryType : byte
{
    EBook = 1 << 0,
    Audiobook = 1 << 1,
    Both = EBook | Audiobook,
    None = 0,
}

public enum Status
{
    Missing = 0, // Book has been added but release was not found yet
    Grabbed = 1, // Release was sent to download client
    Downloading = 2, // Download client has reported the book is added and downloading
    Downloaded = 3, // Book has been downloaded but not yet imported
    Imported = 4, // Book has been imported into the library
}