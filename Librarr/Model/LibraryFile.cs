namespace Librarr.Model;

public class LibraryFile
{
    public          int            Id               { get; init; }
    public required FileType       Type             { get; init; }
    public required DownloadStatus Status           { get; set; }
    public          string?        Format           { get; set; }
    public          string?        TorrentHash      { get; set; }
    public          string?        SourcePath       { get; set; }
    public          List<string>   DestinationFiles { get; set; } = [];

    public required Book Book { get; init; }

    public enum FileType
    {
        Ebook,
        Audiobook
    }

    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Downloaded,
        Imported,
        Failed
    }
}