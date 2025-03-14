using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cheesarr.Model;

public class FileEntry
{
    [Key]      public int        Id     { get; set; }
    [Required] public string     Path   { get; set; } = string.Empty;
    [Required] public string     Format { get; set; } = string.Empty; // TODO: Enum?
}