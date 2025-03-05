using System.ComponentModel.DataAnnotations;

namespace Cheesarr.Settings;

public class ProwlarrSettingsData
{
    [Required(ErrorMessage = "API Key is required")]
    public string APIKey { get; set; }
    
    [Required(ErrorMessage = "API Key is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string Url { get; set; }
}