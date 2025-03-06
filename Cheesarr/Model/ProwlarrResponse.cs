namespace Cheesarr.Model;

public class ProwlarrResponse; // Only really here so I can CMD + O here


public class ProwlarrItem
{
    public string         guid         { get; set; }
    public int            age          { get; set; }
    public double         ageHours     { get; set; }
    public double         ageMinutes   { get; set; }
    public int            size         { get; set; }
    public int            files        { get; set; }
    public int            grabs        { get; set; }
    public int            indexerId    { get; set; }
    public string         indexer      { get; set; }
    public string         title        { get; set; }
    public string         sortTitle    { get; set; }
    public int            imdbId       { get; set; }
    public int            tmdbId       { get; set; }
    public int            tvdbId       { get; set; }
    public int            tvMazeId     { get; set; }
    public DateTime       publishDate  { get; set; }
    public string         downloadUrl  { get; set; }
    public string         infoUrl      { get; set; }
    public List<string>   indexerFlags { get; set; }
    public List<Category> categories   { get; set; }
    public int            seeders      { get; set; }
    public int            leechers     { get; set; }
    public string         protocol     { get; set; }
    public string         fileName     { get; set; }
    
    public class Category
    {
        public int          id            { get; set; }
        public string       name          { get; set; }
        public List<object> subCategories { get; set; }
    }
}