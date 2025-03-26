namespace Librarr.Services.Download;

public interface IDownloadService
{
    public Task AddTorrent(byte[] torrentData, string torrentHash);

    public Task<TorrentItem[]> FetchTorrents(IEnumerable<string> hashes);

    public Task<bool> TestConnection();
}