using System.Text;

namespace Librarr.Utils;

public static class FileUtils
{
    public static string SanitizePathName(string name, string replacement = "_")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty or whitespace.");

        var invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        var result = System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, replacement);

        // Ensure it's not empty after sanitization
        if (string.IsNullOrEmpty(result))
        {
            throw new Exception("Folder name cannot be empty or whitespace.");
        }

        return result;
    }
}