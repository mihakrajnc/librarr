namespace Librarr.Services.ReleaseSearch;

public record ReleaseSearchItem(
    string Title,
    string DownloadURL,
    string InfoURL,
    int Downloads,
    int Seeders,
    int Leechers
);