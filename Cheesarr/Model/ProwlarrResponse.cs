namespace Cheesarr.Model;

// ReSharper disable InconsistentNaming
[Serializable]
public record ProwlarrSearchResponseItem(
    string guid,
    int age,
    double ageHours,
    double ageMinutes,
    int size,
    int files,
    int grabs,
    int indexerId,
    string indexer,
    string title,
    string sortTitle,
    int imdbId,
    int tmdbId,
    int tvdbId,
    int tvMazeId,
    DateTime publishDate,
    string downloadUrl,
    string infoUrl,
    List<string> indexerFlags,
    List<ProwlarrSearchResponseItem.Category> categories,
    int seeders,
    int leechers,
    string protocol,
    string fileName)
{
    [Serializable]
    public record Category(
        int id,
        string name,
        List<object> subCategories
    );
}