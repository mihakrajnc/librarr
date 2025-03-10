using System.Net.Http.Headers;
using Cheesarr.Settings;

namespace Cheesarr.Services;

public class QBTService(HttpClient httpClient, SettingsService ss, ILogger<OpenLibraryService> logger)
{
    private const string ADD_API = "/api/v2/torrents/add";
    
    public async Task DownloadTorrent(string torrentUrl)
    {
        var qbtSettings = ss.GetSettings<QBTSettingsData>();
        var prowlarrSettings = ss.GetSettings<ProwlarrSettingsData>();
        
        // Update the host to the one from settings
        torrentUrl =  new UriBuilder(torrentUrl)
        {
            Host = "172.20.0.1", // TODO: prowlarrSettings.Host
            Port = prowlarrSettings.Port
        }.ToString();
        
        var formData = new Dictionary<string, string>
        {
            { "urls", torrentUrl },
            { "paused", "true" }
        };

        if (!string.IsNullOrEmpty(qbtSettings.Category))
        {
            formData.Add("category", qbtSettings.Category);
        }

        var content = new FormUrlEncodedContent(formData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        await httpClient.PostAsync(ADD_API, content);
    }
}