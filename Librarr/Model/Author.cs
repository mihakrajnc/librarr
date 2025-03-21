namespace Librarr.Model;

public class Author
{
    public          int    Id   { get; init; }
    public required string OLID { get; init; }
    public required string Name { get; init; }

    public List<Book> Books { get; init; } = [];
}