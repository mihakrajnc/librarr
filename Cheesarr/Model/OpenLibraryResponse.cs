namespace Cheesarr.Model;

// ReSharper disable InconsistentNaming
public record OpenLibraryResponse(
    OpenLibraryResponse.Document[] docs
    // int numFound,
    // int start,
    // bool numFoundExact,
    // int num_found,
    // string documentation_url,
    // string q,
    // object offset,
)
{
    public record Document(
        string key,
        string title,
        IReadOnlyList<string> author_key,
        IReadOnlyList<string> author_name,
        string cover_edition_key,
        int first_publish_year
        // int cover_i,
        // int edition_count,
        // bool has_fulltext,
        // IReadOnlyList<string> ia,
        // string ia_collection_s,
        // IReadOnlyList<string> language,
        // bool public_scan_b,
        // string lending_edition_s,
        // string lending_identifier_s,
        // string subtitle
    );
}