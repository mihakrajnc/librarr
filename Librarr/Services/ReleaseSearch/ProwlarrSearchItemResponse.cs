using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Librarr.Services.ReleaseSearch;

[Serializable]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public record ProwlarrSearchItemResponse(
    int grabs,
    string title,
    string downloadUrl,
    string infoUrl,
    int seeders,
    int leechers,
    int indexerId,
    string guid,
    int age,
    double ageHours,
    double ageMinutes,
    int size,
    int files,
    string indexer,
    string sortTitle,
    int imdbId,
    int tmdbId,
    int tvdbId,
    int tvMazeId,
    DateTime publishDate,
    List<string> indexerFlags,
    List<ProwlarrSearchItemResponse.Category> categories,
    string protocol,
    string fileName
)
{
    [Serializable]
    public record Category(
        int id,
        string name,
        List<object> subCategories
    );

    public ReleaseSearchItem ToReleaseSearchItem()
    {
        var firstBracket = title.IndexOf('[');
        var secondBracket = title.IndexOf(']', firstBracket + 1);

        var tagsSplit = title.Substring(firstBracket + 1,
                secondBracket - firstBracket - 1)
            .Split(" / ");

        var languageTag = tagsSplit[0];
        var formatTags = tagsSplit.Length == 1 ? [] : tagsSplit[1].Split(' ').ToImmutableHashSet();

        return new ReleaseSearchItem(
            title,
            downloadUrl,
            infoUrl,
            grabs,
            seeders,
            leechers,
            languageTag,
            formatTags,
            this
        );
    }
}