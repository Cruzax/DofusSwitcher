using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DofusSwitcher.Core;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DofusSwitcher", "config.json");

    public string FilePath { get; }

    public ConfigStore(string filePath) => FilePath = filePath;

    /// <summary>Charge la config ; renvoie une config par défaut si le fichier est absent ou illisible.</summary>
    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppConfig();
            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FilePath), JsonOptions) ?? new AppConfig();
            config.Characters ??= new();
            if (config.ProcessNames is null || config.ProcessNames.Count == 0) config.ProcessNames = new() { "Dofus" };
            return config;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(tempPath, FilePath, overwrite: true);
    }
}
