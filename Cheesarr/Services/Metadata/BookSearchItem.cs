namespace Cheesarr.Services.Metadata;

public record BookSearchItem(
    string ID,
    string Title,
    string AuthorName,
    string AuthorID,
    int PublishYear,
    string CoverURL
);