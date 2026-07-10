using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PKHeX.Application.Abstractions.LiveHex;
using PKHeX.Core;

namespace PKHeX.Presentation.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISaveFileGateway _saveFileService;
    private readonly IDialogService _dialogService;
    private readonly IWindowService _windowService;
    private readonly ISpriteRenderer _spriteRenderer;
    private readonly ISlotService _slotService;
    private readonly IClipboardService _clipboardService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IUpdateCheckService _updateCheckService;
    private readonly AppSettings _settings;
    private readonly ISettingsStore _settingsStore;
    private readonly IThemeService _themeService;
    private readonly UndoRedoService _undoRedo;
    private readonly LanguageService _languageService;
    private readonly IAutoLegalityService _autoLegalityService;
    private readonly ILiveHexService _liveHexService;
    private readonly ILivingDexService _livingDexService;
    private readonly string _currentVersion;

    [ObservableProperty] private UpdateNotificationViewModel? _updateNotification;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSave))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveFileAsCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloseFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportShowdownCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportShowdownCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPKMDatabaseCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenBoxReportCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenAutoLegalityModCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenLiveHeXCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenLivingDexGeneratorCommand))]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    [NotifyCanExecuteChangedFor(nameof(RedoCommand))]
    private SaveFile? _currentSave;

    [ObservableProperty] private BoxViewerViewModel? _boxViewer;
    [ObservableProperty] private PartyViewerViewModel? _partyViewer;
    [ObservableProperty] private TrainerEditorViewModel? _trainerEditor;
    [ObservableProperty] private InventoryEditorViewModel? _inventoryEditor;
    [ObservableProperty] private EventFlagsEditorViewModel? _eventFlagsEditor;
    [ObservableProperty] private MysteryGiftEditorViewModel? _mysteryGiftEditor;
    [ObservableProperty] private BatchEditorViewModel? _batchEditor;
    [ObservableProperty] private PokemonEditorViewModel? _currentPokemonEditor;

    public bool HasSave => CurrentSave is not null;
    public bool CanUndo => _undoRedo.CanUndo;
    public bool CanRedo => _undoRedo.CanRedo;

    public string WindowTitle => CurrentSave is not null
        ? $"PKHeX Avalonia - {CurrentSave.Version}"
        : "PKHeX Avalonia";

    public LanguageService LanguageService => _languageService;

    public MainWindowViewModel(
        ISaveFileGateway saveFileService,
        IDialogService dialogService,
        IWindowService windowService,
        ISpriteRenderer spriteRenderer,
        ISlotService slotService,
        IClipboardService clipboardService,
        IQrCodeService qrCodeService,
        IUpdateCheckService updateCheckService,
        AppSettings settings,
        ISettingsStore settingsStore,
        IThemeService themeService,
        UndoRedoService undoRedo,
        LanguageService languageService,
        IAutoLegalityService autoLegalityService,
        ILiveHexService liveHexService,
        ILivingDexService livingDexService)
    {
        _saveFileService = saveFileService;
        _dialogService = dialogService;
        _windowService = windowService;
        _spriteRenderer = spriteRenderer;
        _slotService = slotService;
        _clipboardService = clipboardService;
        _qrCodeService = qrCodeService;
        _updateCheckService = updateCheckService;
        _settings = settings;
        _settingsStore = settingsStore;
        _themeService = themeService;
        _undoRedo = undoRedo;
        _languageService = languageService;
        _autoLegalityService = autoLegalityService;
        _liveHexService = liveHexService;
        _livingDexService = livingDexService;
        _currentVersion = GetCurrentVersion();

        _saveFileService.SaveFileChanged += OnSaveFileChanged;
        _slotService.ViewRequested += OnViewRequested;
        _slotService.SetRequested += OnSetRequested;
        _slotService.DeleteRequested += OnDeleteRequested;
        _slotService.MoveRequested += OnMoveRequested;

        _undoRedo.StateChanged += (_, _) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };
        _undoRedo.UndoPerformed += OnUndoRedoPerformed;
        _undoRedo.RedoPerformed += OnUndoRedoPerformed;

        _languageService.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (CurrentSave is not null)
            GameInfo.FilteredSources = new FilteredGameDataSource(CurrentSave, GameInfo.Sources);

        OnPropertyChanged(string.Empty);
        CurrentPokemonEditor?.RefreshLanguage();
        BoxViewer?.RefreshCurrentBox();
        TrainerEditor?.RefreshLanguage();
        InventoryEditor?.RefreshLanguage();

        // Relay to on-demand dialog ViewModels (e.g. PKM database) that subscribe via the messenger.
        WeakReferenceMessenger.Default.Send(new LanguageChangedMessage(_languageService.CurrentLanguage));
    }

    private void OnSaveFileChanged(SaveFile? sav)
    {
        // Dismiss any modeless tool windows (e.g. the box seek tool) bound to the previous save.
        _windowService.CloseAllTools();
        _boxReport = null;
        _legalityAudit = null;
        _autoLegalityMod = null;
        DisposeLiveHeX();
        _livingDexGenerator = null;

        CurrentSave = sav;
        if (sav is not null)
        {
            try
            {
                _spriteRenderer.Initialize(sav);
                _undoRedo.Initialize(sav);
                GameInfo.FilteredSources = new FilteredGameDataSource(sav, GameInfo.Sources);

                var pokemonEditor = new PokemonEditorViewModel(sav.BlankPKM, sav, _spriteRenderer, _dialogService, _windowService);
                pokemonEditor.SaveFileDropRequested += OnSaveFileDropRequested;
                CurrentPokemonEditor = pokemonEditor;

                var boxViewer = new BoxViewerViewModel(sav, _spriteRenderer, _slotService, _windowService, _dialogService);
                boxViewer.SlotActivated += OnBoxSlotActivated;
                boxViewer.ViewSlotRequested += OnBoxViewSlot;
                boxViewer.SetSlotRequested += OnBoxSetSlot;
                boxViewer.DeleteSlotRequested += OnBoxDeleteSlot;
                boxViewer.SaveFileDropRequested += OnSaveFileDropRequested;
                BoxViewer = boxViewer;

                var partyViewer = new PartyViewerViewModel(sav, _spriteRenderer, _slotService, _dialogService);
                partyViewer.SlotActivated += OnPartySlotActivated;
                partyViewer.ViewSlotRequested += OnPartyViewSlot;
                partyViewer.SetSlotRequested += OnPartySetSlot;
                partyViewer.SaveFileDropRequested += OnSaveFileDropRequested;
                PartyViewer = partyViewer;

                TrainerEditor = new TrainerEditorViewModel(sav);
                InventoryEditor = new InventoryEditorViewModel(sav, _spriteRenderer);
                EventFlagsEditor = new EventFlagsEditorViewModel(sav);
                MysteryGiftEditor = new MysteryGiftEditorViewModel(sav, _dialogService);
                BatchEditor = new BatchEditorViewModel(sav, _dialogService);
                BatchEditor.BatchEditCompleted += OnBatchEditCompleted;
            }
            catch (Exception ex)
            {
                _saveFileService.CloseSave();
                _ = _dialogService.ShowErrorAsync("Failed to open save file",
                    $"This save file loaded successfully but the editor could not display it.\n\n{ex.GetType().Name}: {ex.Message}");
            }
        }
        else
        {
            CurrentPokemonEditor = null;

            if (BoxViewer is not null)
            {
                BoxViewer.SlotActivated -= OnBoxSlotActivated;
                BoxViewer.ViewSlotRequested -= OnBoxViewSlot;
                BoxViewer.SetSlotRequested -= OnBoxSetSlot;
                BoxViewer.DeleteSlotRequested -= OnBoxDeleteSlot;
                BoxViewer.SaveFileDropRequested -= OnSaveFileDropRequested;
            }
            BoxViewer = null;

            if (PartyViewer is not null)
            {
                PartyViewer.SlotActivated -= OnPartySlotActivated;
                PartyViewer.ViewSlotRequested -= OnPartyViewSlot;
                PartyViewer.SetSlotRequested -= OnPartySetSlot;
                PartyViewer.SaveFileDropRequested -= OnSaveFileDropRequested;
            }
            PartyViewer = null;

            TrainerEditor = null;
            InventoryEditor = null;
            EventFlagsEditor = null;
            MysteryGiftEditor = null;

            if (BatchEditor is not null)
            {
                BatchEditor.BatchEditCompleted -= OnBatchEditCompleted;
                BatchEditor = null;
            }
        }
    }

    /// <summary>
    /// Fire-and-forget from the host at startup (never awaited there, so the main window is never
    /// blocked). Offline/rate-limited failures resolve to a null release list from
    /// <see cref="IUpdateCheckService"/> and are handled silently — no error dialog, no retry.
    /// </summary>
    public async Task CheckForUpdatesAsync()
    {
        var startup = _settings.Startup;
        var previousVersion = startup.Version;
        var showChangelogOnUpgrade = startup.ShowChangelogOnUpdate
            && SemanticVersion.TryParse(previousVersion, out var previous)
            && SemanticVersion.TryParse(_currentVersion, out var current)
            && current > previous;

        // Record that this version has now been run, so the "just upgraded" changelog fires only once.
        if (!string.Equals(previousVersion, _currentVersion, StringComparison.Ordinal))
        {
            startup.Version = _currentVersion;
            _settingsStore.Save(_settings);
        }

        if (!startup.CheckForUpdatesOnStartup)
            return; // Settings toggle disabled: zero network calls.

        var releases = await _updateCheckService.GetReleasesAsync();
        if (releases is null || releases.Count == 0)
            return; // Offline/rate-limited/malformed — already logged by the service; stay silent.

        if (showChangelogOnUpgrade)
        {
            var upgradeNotes = UpdateAvailabilityEvaluator.GetReleasesNewerThan(releases, previousVersion);
            if (upgradeNotes.Count > 0)
                await _windowService.ShowDialogAsync(new UpdateChangelogViewModel(upgradeNotes), "What's New");
        }

        var latest = UpdateAvailabilityEvaluator.GetLatestRelease(releases);
        if (latest is null)
            return;
        if (!UpdateAvailabilityEvaluator.ShouldNotify(_currentVersion, latest.TagName, startup.SkippedUpdateVersion))
            return;

        var newerReleases = UpdateAvailabilityEvaluator.GetReleasesNewerThan(releases, _currentVersion);
        var notification = new UpdateNotificationViewModel(
            newerReleases.Count > 0 ? newerReleases : [latest], _windowService, _settings, _settingsStore);
        notification.Dismissed += () => UpdateNotification = null;
        UpdateNotification = notification;
    }

    private static string GetCurrentVersion()
    {
        // Looked up by assembly name so this Presentation-layer type never references the host
        // assembly directly (same trick used by AboutViewModel).
        var asm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, "PKHeX.Avalonia", StringComparison.Ordinal));
        var version = asm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? asm?.GetName().Version?.ToString(3)
                      ?? "0.0.0";
        return version.Contains('+') ? version[..version.IndexOf('+')] : version;
    }
}
