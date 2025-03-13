using System.Text.Json;
using Cheesarr.Model;
using Cheesarr.Settings;

namespace Cheesarr.Services;

public class QBTService(HttpClient httpClient, SettingsService ss, ILogger<OpenLibraryService> logger)
{
    private const string ADD_API = "/api/v2/torrents/add";
    private const string INFO_API = "/api/v2/torrents/info";

    public async Task AddTorrent(byte[] torrentData, string torrentHash)
    {
        var qbtSettings = ss.GetSettings<QBTSettingsData>();

        // Add torrent to QBT
        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(torrentData), "torrents", "file.torrent" },
            { new StringContent("true"), "paused" }
        };

        if (!string.IsNullOrEmpty(qbtSettings.Category))
        {
            content.Add(new StringContent(qbtSettings.Category), "category");
        }

        var addResponse = await httpClient.PostAsync(ADD_API, content);

        if (!addResponse.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to add torrent: {addResponse.StatusCode}");
            throw new Exception($"Failed to add torrent: {addResponse.StatusCode}");
        }

        logger.LogInformation($"Added torrent with hash: {torrentHash}");
    }

    public async Task<QBTTorrentInfoResponse[]> GetTorrents(IEnumerable<string> hashes)
    {
        var results =
            await httpClient.GetFromJsonAsync<QBTTorrentInfoResponse[]>(
                $"{INFO_API}?hashes={string.Join('|', hashes)}") ?? [];

        return results;
    }
}