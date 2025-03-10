using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Cheesarr.Model;

[Index(nameof(OLID), IsUnique = true)]
public class AuthorEntry
{
    [Key] public int Id { get; set; }

    [Required] public string OLID { get; set; }

    [Required] public string Name { get; set; }

    public List<BookEntry> Books { get; set; }
}