namespace Cheesarr.Services.Metadata;

public class OpenLibraryMetadataService(HttpClient httpClient, ILogger<OpenLibraryMetadataService> logger)
    : IMetadataService
{
    private const string SEARCH_API_URL = "search.json?q={0}";


    public async Task<BookSearchItem[]> Search(string query)
    {
        var uri = string.Format(SEARCH_API_URL, Uri.EscapeDataString(query + " language:eng"));

        logger.LogInformation($"Searching for books from {uri}");

        var response = await httpClient.GetAsync(uri);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to find torrents: {response.StatusCode}");
            throw new Exception($"Failed to find torrents: {response.StatusCode}");
        }

        var responseData = await response.Content.ReadFromJsonAsync<OpenLibrarySearchResponse>();

        if (responseData is not { docs.Length: > 0 }) return [];

        logger.LogInformation($"Found {responseData.docs.Length} matching books");
        return responseData.docs.Select(d => d.ToBookSearchItem()).ToArray();
    }

    // ReSharper disable InconsistentNaming
    private record OpenLibrarySearchResponse(
        OpenLibrarySearchResponse.Document[] docs
    )
    {
        public record Document(
            string key,
            string title,
            IReadOnlyList<string> author_key,
            IReadOnlyList<string> author_name,
            string cover_edition_key,
            int first_publish_year
        )
        {
            public BookSearchItem ToBookSearchItem()
            {
                return new BookSearchItem(
                    key.Replace("/works/", ""),
                    title,
                    author_name.First(),
                    author_key.First(),
                    first_publish_year
                );
            }
        }
    }
}