using Librarr.Services.ReleaseSearch;

namespace Librarr.Model;

// ReSharper disable InconsistentNaming
[Serializable]
public record ProwlarrSearchItemResponse(
    int grabs,
    string title,
    string downloadUrl,
    string infoUrl,
    int seeders,
    int leechers
    // int indexerId,
    // string guid,
    // int age,
    // double ageHours,
    // double ageMinutes,
    // int size,
    // int files,
    // string indexer,
    // string sortTitle,
    // int imdbId,
    // int tmdbId,
    // int tvdbId,
    // int tvMazeId,
    // DateTime publishDate,
    // List<string> indexerFlags,
    // List<ProwlarrSearchItemResponse.Category> categories,
    // string protocol,
    // string fileName
)
{
    // [Serializable]
    // public record Category(
    //     int id,
    //     string name,
    //     List<object> subCategories
    // );

    public ReleaseSearchItem ToReleaseSearchItem()
    {
        return new ReleaseSearchItem(
            title,
            downloadUrl,
            infoUrl,
            grabs,
            seeders,
            leechers
        );
    }
}