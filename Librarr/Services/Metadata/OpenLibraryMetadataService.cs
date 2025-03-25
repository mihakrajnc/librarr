namespace Librarr.Services.Metadata;

public class OpenLibraryMetadataService(HttpClient httpClient, ILogger<OpenLibraryMetadataService> logger)
    : IMetadataService
{
    private const string SEARCH_API_URL = "/search.json?q={0}";
    private const string AUTHOR_API_URL = "/authors/{0}/works.json";
    private const string WORK_API_URL = "/works/{0}.json";

    public async Task<BookSearchItem[]> Search(string query, CancellationToken ct = default)
    {
        var uri = string.Format(SEARCH_API_URL, Uri.EscapeDataString(query));

        logger.LogInformation($"Searching for books (OpenLibrary) with query: {query}");

        var response = await httpClient.GetAsync(uri, ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to find torrents: {response.StatusCode}");
            throw new Exception($"Failed to find torrents: {response.StatusCode}");
        }

        var responseData = await response.Content.ReadFromJsonAsync<OpenLibrarySearchResponse>(ct);

        if (responseData is not { docs.Length: > 0 }) return [];

        logger.LogInformation($"Found {responseData.docs.Length} matching books");
        return responseData.docs
            .Select(ParseBookSearchItem)
            .OfType<BookSearchItem>()
            .ToArray();
    }

    public async Task<BookSearchItem[]> FetchAuthorBooks(string authorKey, string authorName,
        CancellationToken ct = default)
    {
        var uri = string.Format(AUTHOR_API_URL, authorKey);

        logger.LogInformation($"Searching for books (OpenLibrary) for author: {authorKey}");

        var response = await httpClient.GetAsync(uri, ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to find torrents: {response.StatusCode}");
            throw new Exception($"Failed to find torrents: {response.StatusCode}");
        }

        var responseData = await response.Content.ReadFromJsonAsync<OpenLibraryAuthorResponse>(ct);

        if (responseData is not { entries.Length: > 0 }) return [];

        logger.LogInformation($"Found {responseData.entries.Length} matching books");
        return responseData.entries
            .Select(e => ParseBookSearchItem(e, authorKey, authorName))
            .OfType<BookSearchItem>()
            .ToArray();
    }

    public async Task<BookMetadata> FetchBookMetadata(string id, CancellationToken ct = default)
    {
        var uri = string.Format(WORK_API_URL, id);

        logger.LogInformation($"Searching for metadata for book: {id}");

        var response = await httpClient.GetAsync(uri, ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to find metadata: {response.StatusCode}");
            throw new Exception($"Failed to find metadata: {response.StatusCode}");
        }

        var responseData = await response.Content.ReadFromJsonAsync<OpenLibraryWorkResponse>(ct);

        return new BookMetadata(responseData?.description.value ?? "Missing description"); // TODO: Maybe do this better
    }

    private BookSearchItem? ParseBookSearchItem(OpenLibrarySearchResponse.Document doc)
    {
        try
        {
            return new BookSearchItem(
                doc.key.Replace("/works/", ""),
                doc.title,
                doc.author_name.First(),
                doc.author_key.First(),
                doc.first_publish_year,
                $"https://covers.openlibrary.org/b/olid/{doc.cover_edition_key}-M.jpg"
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Failed to parse OpenLibrary search item: {doc.key} - {doc.title}");
            return null;
        }
    }

    private BookSearchItem? ParseBookSearchItem(OpenLibraryAuthorResponse.Entry doc, string authorKey,
        string authorName)
    {
        try
        {
            return new BookSearchItem(
                doc.key.Replace("/works/", ""),
                doc.title,
                authorName,
                authorKey,
                int.Parse(doc.first_publish_date ?? "0"),
                $"https://covers.openlibrary.org/b/olid/FIXME-M.jpg"
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Failed to parse OpenLibrary search item: {doc.title}");
            return null;
        }
    }

    // ReSharper disable InconsistentNaming
    [Serializable]
    private record OpenLibrarySearchResponse(
        OpenLibrarySearchResponse.Document[] docs
    )
    {
        public record Document(
            string key,
            string title,
            string[] author_key,
            string[] author_name,
            string cover_edition_key,
            int first_publish_year
        );
    }

    [Serializable]
    public record OpenLibraryAuthorResponse(
        int size,
        OpenLibraryAuthorResponse.Entry[] entries
    )
    {
        public record Entry(
            object description,
            string title,
            int[] covers,
            string[] subject_places,
            string[] subjects,
            string[] subject_people,
            string key,
            string[] subject_times,
            int latest_revision,
            int revision,
            string? first_publish_date
        );
    }

    [Serializable]
    public record OpenLibraryWorkResponse(
        OpenLibraryWorkResponse.Description description
    )
    {
        public record Description(
            string type,
            string value
        );
    }
}