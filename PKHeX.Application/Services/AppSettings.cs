using System.Text.Json;
using System.Text.Json.Serialization;
using PKHeX.Core;

namespace PKHeX.Application.Services;

/// <summary>
/// Application-layer settings model. Implements Core's <see cref="IProgramSettings"/>. Plain POCO
/// (no MVVM-framework coupling, no file IO) — persistence is handled by an
/// <see cref="Abstractions.ISettingsStore"/> implementation in the Infrastructure layer, and the
/// settings screen binds to <c>SettingsViewModel</c>, which wraps this model.
/// </summary>
public sealed class AppSettings : IProgramSettings
{
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

    /// <summary>
    /// Forward-compatibility bucket: any JSON keys written by a newer version that this build does
    /// not recognize are captured here and re-emitted on save, so upgrading/downgrading does not
    /// silently drop settings. See issue #138 acceptance criteria.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

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
