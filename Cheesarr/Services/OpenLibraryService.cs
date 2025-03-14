using Cheesarr.Data;
using Cheesarr.Model;

namespace Cheesarr.Services;

public class OpenLibraryService(HttpClient httpClient, CheesarrDbContext db, ILogger<OpenLibraryService> logger)
{
    private const string SEARCH_API_URL = "search.json?q={0}";

    public async Task<IReadOnlyList<OLDoc>> SearchBooksAsync(string query)
    {
        var uri = string.Format(SEARCH_API_URL, Uri.EscapeDataString(query));

        logger.LogInformation($"Searching for books from {uri} via {httpClient.BaseAddress}");

        var response = await httpClient.GetFromJsonAsync<OpenLibraryResponse>(uri);

        var docs = response.docs;

        return docs;
    }

    // TODO: Shouldn't really be here
    public async Task<BookEntry> AddBook(OLDoc doc, BookEntryType bookEntryType)
    {
        var authorName = doc.author_name[0]; // TODO: For now we just use the first author
        var authorKey = doc.author_key[0];

        var author = db.Authors.FirstOrDefault(a => a.OLID == authorKey) ?? db.Authors.Add(new AuthorEntry
        {
            OLID = authorKey,
            Name = authorName
        }).Entity;

        var bookEntry = new BookEntry
        {
            OLID = doc.key,
            CoverEditionKey = doc.cover_edition_key,
            Title = doc.title,
            Author = author,
            FirstPublishYear = doc.first_publish_year,
            WantedTypes = bookEntryType,
        };

        db.Books.Add(bookEntry);

        await db.SaveChangesAsync();

        return bookEntry;
    }
}