namespace Librarr.Settings;

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;

// TODO: Memory cache
public class SettingsService(ILogger<SettingsService> logger, string settingsDir)
{
    public void SaveSettings<T>(T settings) where T : class, new()
    {
        var filePath = GetFilePath<T>();
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            logger.LogInformation("Settings {Type} saved to: {Path}", typeof(T).Name, filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving settings for {Type}", typeof(T).Name);
        }
    }

    public T GetSettings<T>() where T : class, new()
    {
        var filePath = GetFilePath<T>();
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json) ?? new T();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving settings for {Type}", typeof(T).Name);
        }

        return new T();
    }

    private string GetFilePath<T>() => Path.Combine(settingsDir, $"{typeof(T).Name.ToLowerInvariant()}.json");
}