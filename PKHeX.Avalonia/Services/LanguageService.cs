using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PKHeX.Core;

namespace PKHeX.Avalonia.Services;

public partial class LanguageService : ObservableObject
{
    private static readonly string[] SupportedLanguages = ["en", "ja", "fr", "it", "de", "es", "ko", "zh-Hans", "zh-Hant"];
    private static readonly string[] LanguageNames = ["English", "日本語", "Français", "Italiano", "Deutsch", "Español", "한국어", "简体中文", "繁體中文"];

    [ObservableProperty]
    private string _currentLanguage = "en";

    public IReadOnlyList<LanguageOption> AvailableLanguages { get; }
    
    public LanguageOption? CurrentLanguageOption
    {
        get => AvailableLanguages.FirstOrDefault(l => l.Code == CurrentLanguage);
        set
        {
            if (value is not null && value.Code != CurrentLanguage)
            {
                SetLanguage(value.Code);
            }
        }
    }

    public event Action? LanguageChanged;

    public LanguageService()
    {
        AvailableLanguages = SupportedLanguages
            .Select((code, i) => new LanguageOption(code, LanguageNames[i]))
            .ToList();
    }

    public void SetLanguage(string languageCode)
    {
        if (!SupportedLanguages.Contains(languageCode))
            languageCode = "en";

        CurrentLanguage = languageCode;
        OnPropertyChanged(nameof(CurrentLanguageOption));
        GameInfo.CurrentLanguage = languageCode;
        GameInfo.Strings = GameInfo.GetStrings(languageCode);
        LanguageChanged?.Invoke();
        WeakReferenceMessenger.Default.Send(new LanguageChangedMessage(languageCode));
    }
}

public record LanguageChangedMessage(string LanguageCode);

public record LanguageOption(string Code, string Name)
{
    public override string ToString() => Name;
}
