namespace Librarr.Model;

public class Book
{
    public int Id { get; init; }

    public required string  OLID             { get; init; }
    public required string  Title            { get; init; }
    public required string? Subtitle         { get; init; }
    public required int     FirstPublishYear { get; init; }
    public required string  CoverURL         { get; init; } // TODO: Not needed if we'll stick with OL
    public required bool    EBookWanted      { get; set; }
    public required bool    AudiobookWanted  { get; set; }

    public required Author            Author { get; init; }
    public          List<LibraryFile> Files  { get; init; } = [];
}