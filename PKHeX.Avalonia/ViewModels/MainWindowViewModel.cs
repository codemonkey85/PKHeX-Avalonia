using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PKHeX.Avalonia.Services;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISaveFileService _saveFileService;
    private readonly IDialogService _dialogService;
    private readonly ISpriteRenderer _spriteRenderer;
    private readonly ISlotService _slotService;
    private readonly IClipboardService _clipboardService;
    private readonly AppSettings _settings;
    private readonly UndoRedoService _undoRedo;
    private readonly LanguageService _languageService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSave))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveFileAsCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloseFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportShowdownCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportShowdownCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPKMDatabaseCommand))]
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
        ISaveFileService saveFileService,
        IDialogService dialogService,
        ISpriteRenderer spriteRenderer,
        ISlotService slotService,
        IClipboardService clipboardService,
        AppSettings settings,
        UndoRedoService undoRedo,
        LanguageService languageService)
    {
        _saveFileService = saveFileService;
        _dialogService = dialogService;
        _spriteRenderer = spriteRenderer;
        _slotService = slotService;
        _clipboardService = clipboardService;
        _settings = settings;
        _undoRedo = undoRedo;
        _languageService = languageService;

        _saveFileService.SaveFileChanged += OnSaveFileChanged;
        _slotService.ViewRequested += OnViewRequested;
        _slotService.SetRequested += OnSetRequested;
        _slotService.DeleteRequested += OnDeleteRequested;
        _slotService.MoveRequested += OnMoveRequested;

        _undoRedo.PropertyChanged += (_, _) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };
        _undoRedo.UndoPerformed += OnUndoRedoPerformed;
        _undoRedo.RedoPerformed += OnUndoRedoPerformed;

        _languageService.LanguageChanged += OnLanguageChanged;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (r, m) => OnLanguageChanged());
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
    }

    private void OnSaveFileChanged(SaveFile? sav)
    {
        CurrentSave = sav;
        if (sav is not null)
        {
            try
            {
                _spriteRenderer.Initialize(sav);
                _undoRedo.Initialize(sav);
                GameInfo.FilteredSources = new FilteredGameDataSource(sav, GameInfo.Sources);

                CurrentPokemonEditor = new PokemonEditorViewModel(sav.BlankPKM, sav, _spriteRenderer, _dialogService);

                var boxViewer = new BoxViewerViewModel(sav, _spriteRenderer, _slotService);
                boxViewer.SlotActivated += OnBoxSlotActivated;
                boxViewer.ViewSlotRequested += OnBoxViewSlot;
                boxViewer.SetSlotRequested += OnBoxSetSlot;
                boxViewer.DeleteSlotRequested += OnBoxDeleteSlot;
                BoxViewer = boxViewer;

                var partyViewer = new PartyViewerViewModel(sav, _spriteRenderer, _slotService);
                partyViewer.SlotActivated += OnPartySlotActivated;
                partyViewer.ViewSlotRequested += OnPartyViewSlot;
                partyViewer.SetSlotRequested += OnPartySetSlot;
                PartyViewer = partyViewer;

                TrainerEditor = new TrainerEditorViewModel(sav);
                InventoryEditor = new InventoryEditorViewModel(sav);
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
            }
            BoxViewer = null;

            if (PartyViewer is not null)
            {
                PartyViewer.SlotActivated -= OnPartySlotActivated;
                PartyViewer.ViewSlotRequested -= OnPartyViewSlot;
                PartyViewer.SetSlotRequested -= OnPartySetSlot;
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
}
