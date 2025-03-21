using System.ComponentModel.DataAnnotations;

namespace Librarr.Settings;

public class ProwlarrSettingsData
{
    [Required(ErrorMessage = "API Key is required")]
    public string? APIKey { get; set; }
    
    [Required(ErrorMessage = "Hostname is required")]
    public string? Host { get; set; }

    [Required(ErrorMessage = "Port is required")]
    public int Port { get; set; } = 9696;
    
    public bool UseSSL { get; set; } = false;
}