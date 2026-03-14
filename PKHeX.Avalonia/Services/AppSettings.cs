using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using PKHeX.Core;

namespace PKHeX.Avalonia.Services;

public partial class AppSettings : ObservableObject, IProgramSettings
{
    private const string ConfigFileName = "config.json";
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

    IStartupSettings IProgramSettings.Startup => Startup;



    // Settings groups
    [ObservableProperty] private StartupSettings _startup = new();
    [ObservableProperty] private BackupSettings _backup = new();
    [ObservableProperty] private SaveLanguageSettings _saveLanguage = new();
    [ObservableProperty] private SlotWriteSettings _slotWrite = new();
    [ObservableProperty] private SetImportSettings _import = new();
    [ObservableProperty] private LegalitySettings _legality = new();
    [ObservableProperty] private EntityConverterSettings _converter = new();
    [ObservableProperty] private LocalResourceSettings _localResources = new();

    public static AppSettings Load()
    {
        if (!File.Exists(ConfigPath))
        {
            return new AppSettings();
        }

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
        catch (Exception)
        {
            // Ignore save errors for now
        }
    }

    [ObservableProperty] private string _displayLanguage = "en";

    public void InitializeCore()
    {
        if (!string.IsNullOrEmpty(DisplayLanguage))
        {
            GameInfo.CurrentLanguage = DisplayLanguage;
        }
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
}
