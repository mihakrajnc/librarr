using Librarr.Settings;

namespace Librarr.Services.Download;

public class QBTDownloadService(HttpClient httpClient, SettingsService ss, ILogger<QBTDownloadService> logger)
    : IDownloadService
{
    private const string ADD_API = "/api/v2/torrents/add";
    private const string INFO_API = "/api/v2/torrents/info";
    private const string VERSION_API = "/api/v2/app/version";

    public async Task AddTorrent(byte[] torrentData, string torrentHash)
    {
        logger.LogInformation("Adding torrent to QBT: {Hash}", torrentHash);

        var qbtSettings = ss.GetSettings<QBTSettingsData>();

        // Add torrent to QBT
        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(torrentData), "torrents", "file.torrent" },
            { new StringContent("true"), "paused" } // TODO: Doesn't work
        };

        if (!string.IsNullOrEmpty(qbtSettings.Category))
        {
            content.Add(new StringContent(qbtSettings.Category), "category");
        }

        var addResponse = await httpClient.PostAsync(ADD_API, content);

        if (!addResponse.IsSuccessStatusCode)
        {
            logger.LogError("Failed to add torrent {Hash}, status code was {StatusCode}", torrentHash,
                addResponse.StatusCode);
            throw new Exception($"Failed to add torrent: {addResponse.StatusCode}");
        }

        logger.LogInformation("Torrent sent to QBT successfully: {Hash}", torrentHash);
    }

    public async Task<TorrentItem[]> FetchTorrents(IEnumerable<string> hashes)
    {
        logger.LogInformation("Fetching torrents from QBT: {Hashes}", hashes);

        var response = await httpClient.GetAsync($"{INFO_API}?hashes={string.Join('|', hashes)}");

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to fetch torrents, status code was {StatusCode}", response.StatusCode);
            throw new Exception($"Failed to fetch torrents: {response.StatusCode}");
        }

        return (await response.Content.ReadFromJsonAsync<QBTTorrentItemResponse[]>())?.Select(r => r.ToTorrentItem())
            .ToArray() ?? [];
    }

    public async Task<bool> TestConnection()
    {
        logger.LogInformation("Testing QBT connection.");

        var response = await httpClient.GetAsync(VERSION_API);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("QBT connection test failed with status code: {StatusCode}", response.StatusCode);
            return false;
        }

        logger.LogInformation("Test succeeded with response: {Response}", await response.Content.ReadAsStringAsync());
        return true;
    }
}