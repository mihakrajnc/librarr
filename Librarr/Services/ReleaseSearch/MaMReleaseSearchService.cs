using System.Net.Http.Headers;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Librarr.Model;
using Serilog.Context;

namespace Librarr.Services.ReleaseSearch;

public class MaMReleaseSearchService(HttpClient httpClient, ILogger<MaMReleaseSearchService> logger)
    : IReleaseSearchService
{
    private const string SEARCH_API_URL = "/tor/js/loadSearchJSONbasic.php";
    private const string DOWNLOAD_API_URL = "/tor/download.php?tid={0}";
    public const string HASH_DOWNLOAD_API_URL = "https://www.myanonamouse.net/tor/download.php/{0}";
    public const string INFO_URL = "https://www.myanonamouse.net/t/{0}";

    public async Task<ReleaseSearchItem[]> Search(string bookName, bool ebooks, bool audiobooks)
    {
        using (LogContext.PushProperty("BookName", bookName))
        {
            // All logs within this block include the RequestId property
            logger.LogInformation("Started book search.");


            var requestData = new List<KeyValuePair<string, string>>
            {
                new("tor[sortType]", "default"),
                new("tor[startNumber]", "0"),
                new("tor[searchType]", "active"), // TODO: Add vip filtering too
                new("dlLink", ""),
                new("description", ""),
                new("perpage", "100"), // TODO: Add pagination if more than 100 results
                new("tor[text]", bookName),
            };

            if (ebooks)
                requestData.Add(new("tor[main_cat][]", "14"));
            if (audiobooks)
                requestData.Add(new("tor[main_cat][]", "13"));

            var content = new FormUrlEncodedContent(requestData);
            var request = new HttpRequestMessage(HttpMethod.Post, SEARCH_API_URL)
            {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Librarr", "0.1"));

            var response = await httpClient.SendAsync(request);
            
            logger.LogInformation("Request done with {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Failed to find torrents: {response.StatusCode}");
                throw new Exception($"Failed to find torrents: {response.StatusCode}");
            }


            return (await response.Content.ReadFromJsonAsync<MaMSearchResponse>())
                ?.data
                .Select(r => r.ToReleaseSearchItem())
                .ToArray() ?? [];
        }
    }

    public async Task<(byte[] data, string hash)> DownloadTorrentFile(string downloadUrl)
    {
        logger.LogInformation($"Downloading torrent from {downloadUrl}");

        // Download the torrent file
        var torrentResponse = await httpClient.GetAsync(downloadUrl);
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