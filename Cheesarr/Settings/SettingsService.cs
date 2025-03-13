namespace Cheesarr.Settings;

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;

// TODO: Memory cache
public class SettingsService
{
    private const string SETTINGS_DIRECTORY = "db";
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        Directory.CreateDirectory(SETTINGS_DIRECTORY); // Ensure settings directory exists
    }

    public void SaveSettings<T>(T settings) where T : class, new()
    {
        var filePath = GetFilePath<T>();
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving settings for {typeof(T).Name}: {ex.Message}");
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
            _logger.LogError($"Error retrieving settings for {typeof(T).Name}: {ex.Message}");
        }

        return new T();
    }

    private static string GetFilePath<T>() =>
        Path.Combine(SETTINGS_DIRECTORY, $"{typeof(T).Name.ToLowerInvariant()}.json");
}