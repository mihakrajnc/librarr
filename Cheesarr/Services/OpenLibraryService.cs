using Cheesarr.Model;

namespace Cheesarr.Services;

public class OpenLibraryService(HttpClient httpClient, ILogger<OpenLibraryService> logger)
{
    private const string SEARCH_API_URL = "search.json?q={0}&fields=key,title,author_name,author_key,first_publish_year";

    public async Task<List<OpenLibraryResponse.Document>> SearchBooksAsync(string query)
    {
        var uri = string.Format(SEARCH_API_URL, Uri.EscapeDataString(query));
        
        logger.LogInformation($"Searching for books from {uri} via {httpClient.BaseAddress}");
        
        var response = await httpClient.GetFromJsonAsync<OpenLibraryResponse>(uri);

        var docs = response.Docs;

        return docs;
    }
}