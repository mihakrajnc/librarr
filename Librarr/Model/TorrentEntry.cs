using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Model;

[Index(nameof(Hash), IsUnique = true)]
public class TorrentEntry
{
    [Key]                     public          int    Id            { get; init; }
    [Required, MaxLength(64)] public required string Hash          { get; init; }
    [MaxLength(255)]          public          string ContentPath   { get; set; } = string.Empty;
    public                                    Status TorrentStatus { get; set; } = Status.Downloading;
    public                                    bool   IsImported    { get; set; } = false;

    public enum Status
    {
        Missing,
        Downloading,
        Downloaded,
    }
}