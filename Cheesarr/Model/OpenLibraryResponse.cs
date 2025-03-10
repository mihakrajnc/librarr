using System.Text.Json.Serialization;

namespace Cheesarr.Model;

public record OLDoc(
    IReadOnlyList<string> author_key,
    IReadOnlyList<string> author_name,
    string cover_edition_key,
    int cover_i,
    int edition_count,
    int first_publish_year,
    bool has_fulltext,
    IReadOnlyList<string> ia,
    string ia_collection_s,
    string key,
    IReadOnlyList<string> language,
    bool public_scan_b,
    string title,
    string lending_edition_s,
    string lending_identifier_s,
    string subtitle
);

public record OpenLibraryResponse(
    int numFound,
    int start,
    bool numFoundExact,
    int num_found,
    string documentation_url,
    string q,
    object offset,
    IReadOnlyList<OLDoc> docs
);
