using System.Text.Json.Serialization;

namespace Cheesarr.Model;

public class OpenLibraryResponse
{
    [JsonPropertyName("start")]     public int            Start    { get; set; }
    [JsonPropertyName("num_found")] public int            NumFound { get; set; }
    [JsonPropertyName("docs")]      public List<Document> Docs     { get; set; } = [];

    public class Document
    {
        [JsonPropertyName("key")]         public string       Key        { get; set; } = string.Empty;
        [JsonPropertyName("title")]       public string       Title      { get; set; } = string.Empty;
        [JsonPropertyName("author_name")] public List<string> AuthorName { get; set; } = [];
        [JsonPropertyName("author_key")]  public List<string> AuthorKey  { get; set; } = [];

        [JsonPropertyName("first_publish_year")]
        public int FirstPublishYear { get; set; }

        [JsonPropertyName("cover_i")]       public int          CoverI       { get; set; }
        [JsonPropertyName("has_fulltext")]  public bool         HasFulltext  { get; set; }
        [JsonPropertyName("edition_count")] public int          EditionCount { get; set; }
        [JsonPropertyName("ia")]            public List<string> Ia           { get; set; } = [];
        [JsonPropertyName("public_scan_b")] public bool         PublicScanB  { get; set; }
    }
}