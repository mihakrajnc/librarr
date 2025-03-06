namespace Cheesarr.Model;

public class BookEntry
{
    public int Id { get; set; }

    public string Key              { get; set; } = string.Empty;
    public string Title            { get; set; } = string.Empty;
    public string Author           { get; set; } = string.Empty; // TODO: There's more than one, separate table maybe
    public int?   FirstPublishYear { get; set; }
    public Status Status           { get; set; } = Status.Wanted;
}

public enum Status
{
    Wanted,
    Grabbed,
    Downloaded,
}