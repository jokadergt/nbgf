using System;
using System.IO;
using System.Text.Json;

namespace RblxExecutorUI;

public sealed class UserPreferences
{
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "pt-BR";
    public string WindowPreset { get; set; } = "Classic";
    public string UiFont { get; set; } = "Segoe UI";
    public string EditorFont { get; set; } = "Consolas";
    public void Normalize()
    {
        if (Theme is not ("Dark" or "Light")) Theme = "Dark";
        if (!UiCatalog.Languages.Any(item => item.Id == Language)) Language = "pt-BR";
        if (WindowPreset is not ("Classic" or "MacOS" or "Compact")) WindowPreset = "Classic";
        if (!UiCatalog.UiFonts.Contains(UiFont)) UiFont = "Segoe UI";
        if (!UiCatalog.CodeFonts.Contains(EditorFont)) EditorFont = "Consolas";
    }
    public bool AutoInject { get; set; }
    public bool StreamMode { get; set; }
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cesi", "settings.json");

    public static UserPreferences Load(string? path = null)
    {
        path ??= SettingsPath;
        try
        {
            var value = File.Exists(path)
                ? JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(path)) ?? new()
                : new();
            value.Normalize();
            return value;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            System.Diagnostics.Debug.WriteLine($"cesi settings: {ex.GetType().Name}");
            return new();
        }
    }

    public void Save(string? path = null)
    {
        path ??= SettingsPath;
        Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, overwrite: true);
    }
}
