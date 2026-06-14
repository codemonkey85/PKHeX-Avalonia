using System.Text.Json;
using PKHeX.Core;

namespace PKHeX.Application.Services;

/// <summary>
/// Application-layer settings model. Implements Core's <see cref="IProgramSettings"/> and persists
/// itself as JSON. Plain POCO (no MVVM-framework coupling) — the settings screen binds to
/// <c>SettingsViewModel</c>, which wraps this model.
/// </summary>
public sealed class AppSettings : IProgramSettings
{
    private const string ConfigFileName = "config.json";
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

    IStartupSettings IProgramSettings.Startup => Startup;

    // Settings groups (Core-defined types; locally-defined where Core only ships an interface).
    public StartupSettings Startup { get; set; } = new();
    public BackupSettings Backup { get; set; } = new();
    public SaveLanguageSettings SaveLanguage { get; set; } = new();
    public SlotWriteSettings SlotWrite { get; set; } = new();
    public SetImportSettings Import { get; set; } = new();
    public LegalitySettings Legality { get; set; } = new();
    public EntityConverterSettings Converter { get; set; } = new();
    public LocalResourceSettings LocalResources { get; set; } = new();
    public SpriteSettings Sprite { get; set; } = new();

    public string DisplayLanguage { get; set; } = "en";

    public static AppSettings Load()
    {
        if (!File.Exists(ConfigPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
        }
        catch (Exception)
        {
            // Fallback to defaults on error
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Failed to save settings: {ex.Message}");
        }
    }

    public void InitializeCore()
    {
        if (!string.IsNullOrEmpty(DisplayLanguage))
            GameInfo.CurrentLanguage = DisplayLanguage;
    }

    /// <summary>
    /// Local implementation of IStartupSettings since Core only defines the interface.
    /// </summary>
    public class StartupSettings : IStartupSettings
    {
        public GameVersion DefaultSaveVersion { get; set; } = GameVersion.SW;
        public SaveFileLoadSetting AutoLoadSaveOnStartup { get; set; } = SaveFileLoadSetting.LastLoaded;
        public System.Collections.Generic.List<string> RecentlyLoaded { get; set; } = [];
        public string Version { get; set; } = string.Empty;
        public bool ShowChangelogOnUpdate { get; set; } = true;
        public bool ForceHaXOnLaunch { get; set; } = false;
    }

    /// <summary>
    /// Sprite display preferences. Defined locally since Core does not expose a sprite settings type.
    /// </summary>
    public class SpriteSettings
    {
        public SpritePreference SpritePreference { get; set; } = SpritePreference.UseSuggested;
    }
}
