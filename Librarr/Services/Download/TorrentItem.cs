namespace Librarr.Services.Download;

public record TorrentItem(
    string Path,
    string Hash,
    bool DownloadCompleted
);