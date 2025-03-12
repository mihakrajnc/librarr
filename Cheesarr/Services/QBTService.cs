using System.Net.Http.Headers;
using System.Security.Cryptography;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Cheesarr.Settings;

namespace Cheesarr.Services;

public class QBTService(HttpClient httpClient, SettingsService ss, ILogger<OpenLibraryService> logger)
{
    private const string ADD_API = "/api/v2/torrents/add";
    
    
    
    public async Task<string?> DownloadTorrent(string torrentUrl)
    {
        logger.LogInformation($"Downloading torrent from {torrentUrl}");
        
        var qbtSettings = ss.GetSettings<QBTSettingsData>();
        
        // Download the torrent file
        using var torrentHttpClient = new HttpClient();
        var torrentResponse = await torrentHttpClient.GetAsync(torrentUrl);
        if (!torrentResponse.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to download torrent: {torrentResponse.StatusCode}");
            return null;
        }

        var torrentData = await torrentResponse.Content.ReadAsByteArrayAsync();
        var torrentHash = ComputeTorrentHash(torrentData);

        logger.LogInformation($"Downloaded torrent with hash: {torrentHash}");
        
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
            return null;
        }
        
        logger.LogInformation($"Added torrent with hash: {torrentHash}");

        return torrentHash;
    }

    private static string ComputeTorrentHash(byte[] torrentBytes)
    {
        var parser = new BencodeParser();
        var torrent = parser.Parse<Torrent>(torrentBytes);

        return torrent.OriginalInfoHash;
    }
}