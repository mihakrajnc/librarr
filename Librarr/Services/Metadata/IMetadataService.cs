namespace Librarr.Services.Metadata;

public interface IMetadataService
{
    public Task<BookSearchItem[]> Search(string query, CancellationToken ct = default);

    public Task<BookSearchItem[]> FetchAuthorBooks(string authorKey, string authorName,
        CancellationToken ct = default);

}