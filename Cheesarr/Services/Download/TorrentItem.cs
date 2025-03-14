namespace Cheesarr.Services.Download;

public record TorrentItem(
    string Path,
    string Hash,
    TorrentItem.DownloadStatus Status
)
{
    public enum DownloadStatus
    {
        Downloading,
        Downloaded
    }
}