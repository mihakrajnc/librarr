namespace Cheesarr.Services.Metadata;

public interface IMetadataService
{
    public Task<BookSearchItem[]> Search(string query);
}