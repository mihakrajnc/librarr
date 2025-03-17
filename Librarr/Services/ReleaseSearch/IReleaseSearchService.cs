namespace Librarr.Services.ReleaseSearch;

public interface IReleaseSearchService
{
    public Task<ReleaseSearchItem[]> Search(string bookName, bool ebooks, bool audiobooks);

    public Task<(byte[] data, string hash)> DownloadTorrentFile(string downloadUrl);
}