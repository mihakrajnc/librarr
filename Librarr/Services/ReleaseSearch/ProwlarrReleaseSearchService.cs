using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Librarr.Model;

namespace Librarr.Services.ReleaseSearch;

public class ProwlarrReleaseSearchService(HttpClient httpClient, ILogger<ProwlarrReleaseSearchService> logger)
    : IReleaseSearchService
{
    private const string SEARCH_API_URL = "api/v1/search?query={0}";

    private const int CAT_EBOOKS = 7020;
    private const int CAT_AUDIOBOOKS = 3030;

    public async Task<ReleaseSearchItem[]> Search(string bookName, bool ebooks, bool audiobooks)
    {
        var url = string.Format(SEARCH_API_URL, Uri.EscapeDataString(bookName));

        if (ebooks) url += $"&categories={CAT_EBOOKS}";
        if (audiobooks) url += $"&categories={CAT_AUDIOBOOKS}";

        var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to find torrents: {response.StatusCode}");
            throw new Exception($"Failed to find torrents: {response.StatusCode}");
        }

        return (await response.Content.ReadFromJsonAsync<ProwlarrSearchItemResponse[]>())
            ?.Select(r => r.ToReleaseSearchItem())
            .ToArray() ?? [];
    }

    public async Task<(byte[] data, string hash)> DownloadTorrentFile(string downloadUrl)
    {
        logger.LogInformation($"Downloading torrent from {downloadUrl}");

        // Download the torrent file
        using var torrentHttpClient = new HttpClient();
        var torrentResponse = await torrentHttpClient.GetAsync(downloadUrl);
        if (!torrentResponse.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to download torrent: {torrentResponse.StatusCode}");
            throw new Exception($"Failed to download torrent: {torrentResponse.StatusCode}");
        }

        var torrentData = await torrentResponse.Content.ReadAsByteArrayAsync();
        var torrentHash = new BencodeParser().Parse<Torrent>(torrentData).OriginalInfoHash.ToLowerInvariant();

        logger.LogInformation($"Downloaded torrent with hash: {torrentHash}");

        return (torrentData, torrentHash);
    }
}