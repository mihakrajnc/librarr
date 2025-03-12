using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cheesarr.Model;

public class BookEntry
{
    [Key] public int Id { get; set; }

    [Required] public string   Key              { get; set; } = string.Empty;
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