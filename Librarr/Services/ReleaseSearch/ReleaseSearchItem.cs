using System.Collections.Immutable;

namespace Librarr.Services.ReleaseSearch;

public record ReleaseSearchItem(
    string Title,
    string DownloadURL,
    string InfoURL,
    int Downloads,
    int Seeders,
    int Leechers,
    string Language,
    ImmutableHashSet<string> Formats,
    object Source
);