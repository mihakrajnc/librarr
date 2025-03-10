using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cheesarr.Model;

public class BookEntry
{
    [Key] public int Id { get; set; }

    [Required] public string Key              { get; set; } = string.Empty;
    [Required] public string CoverEditionKey  { get; set; } = string.Empty;
    [Required] public string Title            { get; set; } = string.Empty;
    public            int    FirstPublishYear { get; set; }
    public            Status Status           { get; set; } = Status.Paused;


    public                                int         AuthorId { get; set; }
    [ForeignKey(nameof(AuthorId))] public AuthorEntry Author   { get; set; }

    [Required] public int ProfileId { get; set; }

    [ForeignKey(nameof(ProfileId))] public ProfileEntry Profile { get; set; }
}

public enum Status
{
    Paused,
    Wanted,
    Grabbed,
    Downloaded,
}