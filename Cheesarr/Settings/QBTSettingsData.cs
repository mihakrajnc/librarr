using System.ComponentModel.DataAnnotations;

namespace Cheesarr.Settings;

public class QBTSettingsData
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Host is required")]
    public string Host { get; set; } = "localhost";

    [Required(ErrorMessage = "Port is required")]
    public int Port { get; set; } = 8090;
    
    public bool UseSSL { get; set; } = false;

    public string Category { get; set; } = string.Empty;
}