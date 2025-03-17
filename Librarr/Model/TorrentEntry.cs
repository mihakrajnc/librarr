using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Model;

[Index(nameof(Hash), IsUnique = true)]
public class TorrentEntry
{
    [Key]                     public int       Id            { get; set; }
    [Required, MaxLength(64)] public string    Hash          { get; set; } = string.Empty;
    [MaxLength(255)]          public string    ContentPath   { get; set; } = string.Empty;
    public                           Status    TorrentStatus { get; set; } = Status.Downloading;

    public enum Status
    {
        Missing,
        Downloading,
        Downloaded,
        Imported
    }
}