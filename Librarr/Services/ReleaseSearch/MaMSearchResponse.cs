using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Librarr.Services.ReleaseSearch;

[Serializable]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public record MaMSearchResponse(
    int perpage,
    int start,
    MaMSearchResponse.MaMSearchItem[] data,
    int total,
    int found)
{
    [Serializable]
    public record MaMSearchItem(
        int id,
        int language,
        string lang_code,
        int main_cat,
        int category,
        string catname,
        string size,
        int numfiles,
        int vip,
        int free,
        int personal_freeleech,
        int fl_vip,
        string title,
        int w,
        string tags,
        string author_info,
        string narrator_info,
        string series_info,
        string filetype,
        int seeders,
        int leechers,
        string added,
        int browseflags,
        int times_completed,
        int comments,
        string owner_name,
        int owner,
        object bookmarked,
        int my_snatched,
        string description,
        string cat,
        string dl
    )
    {
        public ReleaseSearchItem ToReleaseSearchItem()
        {
            return new ReleaseSearchItem(
                title,
                string.Format(MaMReleaseSearchService.HASH_DOWNLOAD_API_URL, dl),
                string.Format(MaMReleaseSearchService.INFO_URL, id),
                times_completed,
                seeders,
                leechers,
                lang_code,
                filetype.Split(' ').Select(f => f.ToUpperInvariant()).ToImmutableHashSet(),
                this
            );
        }
    }
}