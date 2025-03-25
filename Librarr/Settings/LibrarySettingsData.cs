using System.ComponentModel.DataAnnotations;

namespace Librarr.Settings;

public class LibrarySettingsData
{
    [Required(ErrorMessage = "Library path is required")]
    public string? LibraryPath { get; set; }

    public bool CreateHardLinks { get; set; } = true;
}