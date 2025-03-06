using Cheesarr.Model;
using Cheesarr.Settings;

namespace Cheesarr.Services;

public class ProwlarrService(HttpClient httpClient, SettingsService settings, ILogger<OpenLibraryService> logger)
{
    private const string SEARCH_API_URL = "search?query={0}";

    private const int CAT_EBOOKS = 7020;
    private const int CAT_AUDIOBOOKS = 3030;

    public Task<ProwlarrItem[]> Search(string bookName, bool ebooks, bool audiobooks)
    {
        var url = string.Format(SEARCH_API_URL, Uri.EscapeDataString(bookName));

        if (ebooks) url += $"&categories={CAT_EBOOKS}";
        if (audiobooks) url += $"&categories={CAT_AUDIOBOOKS}";

        return httpClient.GetFromJsonAsync<ProwlarrItem[]>(url)!;
    }
}