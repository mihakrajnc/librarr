using System.ComponentModel.DataAnnotations;

namespace Cheesarr.Model;

public class BookEntry
{
    [Key, MaxLength(20)]  public required string ID             { get; init; }
    [Required, MaxLength(20)]  public required string CoverEditionKey  { get; init; }
    [Required, MaxLength(255)] public required string Title            { get; init; }
    public                                     int    FirstPublishYear { get; init; }

    public BookEntryType WantedTypes     { get; set; } = BookEntryType.None;
    public Status        EBookStatus     { get; set; } = Status.Missing;
    public Status        AudiobookStatus { get; set; } = Status.Missing;

    public required AuthorEntry   Author           { get; init; }
    public          TorrentEntry? EBookTorrent     { get; set; }
    public          TorrentEntry? AudiobookTorrent { get; set; }
    public          FileEntry?    EBookFile        { get; set; }
    public          FileEntry?    AudiobookFile    { get; set; }
}

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