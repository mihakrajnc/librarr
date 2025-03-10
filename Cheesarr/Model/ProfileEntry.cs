using System.ComponentModel.DataAnnotations;

namespace Cheesarr.Model;

public class ProfileEntry
{
    public int Id { get; set; }

    [Required] public string Name { get; set; } = string.Empty;
    
    [Required] public BookFormat[] Formats { get; set; } = [];

    public List<BookEntry> BookEntries { get; set; } = [];
}

[Flags]
public enum BookFormat
{
    FLAC,
    M4B,
    MP3,
    AZW3,
    EPUB,
    MOBI
}