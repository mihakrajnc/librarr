using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cheesarr.Model;

public class FileEntry
{
    [Key]      public          int    Id     { get; set; }
    [Required] public required string Path   { get; set; }
    [Required, MaxLength(4)] public required string Format { get; set; } // TODO: Enum?
}