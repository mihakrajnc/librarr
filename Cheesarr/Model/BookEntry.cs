using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cheesarr.Model;

[Index(nameof(OLID), IsUnique = true)]
public class BookEntry
{
    [Key] public int Id { get; set; }

    [Required] public string   OLID              { get; set; } = string.Empty;
    [Required] public string   CoverEditionKey  { get; set; } = string.Empty;
    [Required] public string   Title            { get; set; } = string.Empty;
    public            int      FirstPublishYear { get; set; }
    public            Status   Status           { get; set; } = Status.Wanted;
    public            GrabType GrabType         { get; set; }
    
    public string EBookTorrentHash { get; set; } = string.Empty;


    public                                int         AuthorId { get; set; }
    [ForeignKey(nameof(AuthorId))] public AuthorEntry Author   { get; set; }
}

public enum GrabType
{
    EBook,
    Audiobook,
    Both,
}

public enum Status
{
    Wanted,
    Grabbed,
    Downloaded,
}