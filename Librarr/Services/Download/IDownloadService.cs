namespace Librarr.Services.Download;

public interface IDownloadService
{
    public Task AddTorrent(byte[] torrentData, string torrentHash);

    public Task<TorrentItem[]> GetTorrents(IEnumerable<string> hashes);

    public Task<bool> TestConnection();
}