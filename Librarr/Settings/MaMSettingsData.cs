using System.ComponentModel.DataAnnotations;

namespace Librarr.Settings;

public class MaMSettingsData
{
    [Required(ErrorMessage = "BaseURL is required")]
    public string BaseURL { get; set; } = "https://www.myanonamouse.net";
    
    [Required(ErrorMessage = "MaM ID is required")]
    public string MaMID { get; set; } = string.Empty;
}