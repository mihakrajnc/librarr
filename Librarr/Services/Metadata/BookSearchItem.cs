namespace Librarr.Services.Metadata;

public record BookSearchItem(
    string ID,
    string Title,
    string? Subtitle,
    string AuthorName,
    string AuthorID,
    int PublishYear,
    string CoverURL
);