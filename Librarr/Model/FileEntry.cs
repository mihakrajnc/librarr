using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Librarr.Model;

public class FileEntry
{
    [Key]      public          int    Id     { get; set; }
    [Required] public required string[] Paths   { get; set; }
    [Required, MaxLength(4)] public required string Format { get; set; } // TODO: Enum?
}