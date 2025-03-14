using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Cheesarr.Model;

[Index(nameof(Hash), IsUnique = true)]
public class TorrentEntry
{
    [Key]                     public int       Id            { get; set; }
    [Required, MaxLength(64)] public string    Hash          { get; set; } = string.Empty;
    [MaxLength(500)]          public string    ContentPath   { get; set; } = string.Empty;
    public                           Status    TorrentStatus { get; set; } = Status.Downloading;
    
    
    // Foreign keys to BookEntry
    public int? EBookOfId { get; set; }
    public BookEntry EBookOf { get; set; }

    public int? AudiobookOfId { get; set; }
    public BookEntry AudiobookOf { get; set; }

    public enum Status
    {
        Missing,
        Downloading,
        Downloaded,
        Imported
    }
}