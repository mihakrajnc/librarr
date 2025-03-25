using System.Diagnostics;
using System.Runtime.InteropServices;

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
    
    public static bool CreateHardLink(string sourcePath, string destinationPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return CreateHardLinkWindows(destinationPath, sourcePath, IntPtr.Zero);
        }
        else
        {
            return CreateHardLinkUnix(sourcePath, destinationPath);
        }
    }

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLinkWindows(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    private static bool CreateHardLinkUnix(string sourcePath, string destinationPath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/ln",
                    Arguments = $"\"{sourcePath}\" \"{destinationPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}