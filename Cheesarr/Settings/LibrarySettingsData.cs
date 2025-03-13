using System.ComponentModel.DataAnnotations;

namespace Cheesarr.Settings;

public class LibrarySettingsData
{
    [Required(ErrorMessage = "Library path is required")]
    public string LibraryPath { get; set; }
    
}