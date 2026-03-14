using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Avalonia.Views;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => _undoRedo.Undo();

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => _undoRedo.Redo();

    [RelayCommand]
    private void ChangeLanguage(string languageCode) => _languageService.SetLanguage(languageCode);

    [RelayCommand]
    private async Task OpenAboutAsync()
    {
        await _dialogService.ShowDialogAsync(new AboutView(), "About PKHeX");
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var vm = new SettingsViewModel(_settings, _languageService);
        var view = new SettingsView { DataContext = vm };
        vm.CloseRequested += () =>
        {
            var window = TopLevel.GetTopLevel(view) as Window;
            window?.Close();
        };
        await _dialogService.ShowDialogAsync(view, "Settings");
    }

    [RelayCommand]
    private async Task OpenFolderListAsync()
    {
        var vm = new FolderListViewModel(_saveFileService, _settings, _dialogService);
        await _dialogService.ShowDialogAsync(new FolderList { DataContext = vm }, "Save Folder List");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBatchEditorAsync()
    {
        if (CurrentSave is null) return;
        var vm = new BatchEditorViewModel(CurrentSave, _dialogService);
        vm.BatchEditCompleted += OnBatchEditCompleted;
        await _dialogService.ShowDialogAsync(new BatchEditor { DataContext = vm }, "Batch Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBlockEditorAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new BlockEditor { DataContext = new BlockEditorViewModel(CurrentSave, _dialogService) },
            "Block Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBoxManipAsync()
    {
        if (CurrentSave is null) return;
        var vm = new BoxManipViewModel(CurrentSave, _dialogService, () => BoxViewer?.RefreshCurrentBox());
        await _dialogService.ShowDialogAsync(new BoxManipView { DataContext = vm }, "Box Manipulation");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenEncounterDatabaseAsync()
    {
        if (CurrentSave is null) return;
        var vm = new EncounterDatabaseViewModel(CurrentSave, _spriteRenderer, _dialogService,
            pk => CurrentPokemonEditor?.LoadPKM(pk));
        await _dialogService.ShowDialogAsync(new EncounterDatabaseView { DataContext = vm }, "Encounter Database");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenGroupViewerAsync()
    {
        if (CurrentSave is null) return;

        System.Collections.Generic.IReadOnlyList<SlotGroup>? groups = null;
        if (CurrentSave is SAV_STADIUM s0)
            groups = s0.GetRegisteredTeams();

        if (groups is null || groups.Count == 0)
        {
            await _dialogService.ShowErrorAsync("Info", "No groups available for this save file.");
            return;
        }

        var vm = new GroupViewerViewModel(CurrentSave, groups, _spriteRenderer, _slotService);
        await _dialogService.ShowDialogAsync(new GroupViewer { DataContext = vm }, "Group Viewer");
    }

    // — Save-specific editors —

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenDaycareAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new DaycareEditorView { DataContext = new DaycareEditorViewModel(CurrentSave, _spriteRenderer) },
            "Daycare");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRecordsAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new RecordsEditorView { DataContext = new RecordsEditorViewModel(CurrentSave) },
            "Game Records");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHallOfFameAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new HallOfFameEditor { DataContext = new HallOfFameEditorViewModel(CurrentSave, _spriteRenderer) },
            "Hall of Fame");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSecretBaseAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new SecretBaseEditor { DataContext = new SecretBaseEditorViewModel(CurrentSave, _spriteRenderer) },
            "Secret Base Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokebeanAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new PokebeanEditor { DataContext = new PokebeanEditorViewModel(CurrentSave) },
            "Poké Bean Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenFestivalPlazaAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new FestivalPlazaEditor { DataContext = new FestivalPlazaEditorViewModel(CurrentSave) },
            "Festival Plaza Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRaidEditorAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new RaidEditor { DataContext = new RaidEditorViewModel(CurrentSave) },
            "Raid Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSuperTrainingAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new SuperTrainingEditor { DataContext = new SuperTrainingEditorViewModel(CurrentSave) },
            "Super Training Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenApricornAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new ApricornEditor { DataContext = new ApricornEditorViewModel(CurrentSave) },
            "Apricorn Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokeGear4Async()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new PokeGear4Editor { DataContext = new PokeGear4EditorViewModel(CurrentSave) },
            "PokéGear Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenGeonet4Async()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new Geonet4Editor { DataContext = new Geonet4EditorViewModel(CurrentSave) },
            "Geonet Globe Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBoxLayoutAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new BoxLayoutEditor { DataContext = new BoxLayoutEditorViewModel(CurrentSave) },
            "Box Layout Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHoneyTreeAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new HoneyTreeEditor { DataContext = new HoneyTreeEditorViewModel(CurrentSave) },
            "Honey Tree Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenUndergroundAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new UndergroundEditor { DataContext = new UndergroundEditorViewModel(CurrentSave) },
            "Underground Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRoamerAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new RoamerEditor { DataContext = new RoamerEditorViewModel(CurrentSave) },
            "Roamer Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenOPowerAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new OPowerEditor { DataContext = new OPowerEditorViewModel(CurrentSave) },
            "O-Power Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenZygardeCellAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new ZygardeCellEditor { DataContext = new ZygardeCellEditorViewModel(CurrentSave) },
            "Zygarde Cell Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRaid9Async()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new Raid9Editor { DataContext = new Raid9EditorViewModel(CurrentSave) },
            "Tera Raid Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRaidSevenStar9Async()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new RaidSevenStar9Editor { DataContext = new RaidSevenStar9EditorViewModel(CurrentSave) },
            "7-Star Tera Raid Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenFashion9Async()
    {
        if (CurrentSave is not (SAV9SV or SAV9ZA)) return;
        await _dialogService.ShowDialogAsync(
            new Fashion9Editor { DataContext = new Fashion9EditorViewModel(CurrentSave!) },
            "Fashion Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenDonutAsync()
    {
        if (CurrentSave is not SAV9ZA sav) return;
        await _dialogService.ShowDialogAsync(
            new DonutEditor { DataContext = new DonutEditorViewModel(sav) },
            "Donut Editor (PLZA)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenDLC5Async()
    {
        if (CurrentSave is not SAV5 sav) return;
        await _dialogService.ShowDialogAsync(
            new DLC5Editor { DataContext = new DLC5EditorViewModel(sav, _dialogService) },
            "DLC Editor (Gen 5)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenEntralinkAsync()
    {
        if (CurrentSave is not SAV5 sav) return;
        await _dialogService.ShowDialogAsync(
            new EntralinkEditor { DataContext = new EntralinkEditorViewModel(sav) },
            "Entralink Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokedexAsync()
    {
        if (CurrentSave is null) return;

        (Control view, string title) = CurrentSave switch
        {
            SAV9SV s    => ((Control)new PokedexGen9Editor { DataContext = new PokedexGen9EditorViewModel(s) },   "Pokédex Editor (Gen 9 SV)"),
            SAV8SWSH s  => (new Pokedex8Editor  { DataContext = new Pokedex8EditorViewModel(s) },                "Pokédex Editor (Gen 8 SwSh)"),
            SAV8BS s    => (new Pokedex8bEditor { DataContext = new Pokedex8bEditorViewModel(s) },               "Pokédex Editor (Gen 8 BDSP)"),
            SAV8LA s    => (new PokedexLAEditor { DataContext = new PokedexLAEditorViewModel(s) },               "Pokédex Editor (PLA)"),
            SAV7b s     => (new Pokedex7bEditor { DataContext = new Pokedex7bEditorViewModel(s) },               "Pokédex Editor (Let's Go)"),
            SAV7 s      => (new Pokedex7Editor  { DataContext = new Pokedex7EditorViewModel(s) },                "Pokédex Editor (Gen 7)"),
            SAV6 s      => (new Pokedex6Editor  { DataContext = new Pokedex6EditorViewModel(s) },                "Pokédex Editor (Gen 6)"),
            SAV5 s      => (new Pokedex5Editor  { DataContext = new Pokedex5EditorViewModel(s) },                "Pokédex Editor (Gen 5)"),
            SAV4 s      => (new Pokedex4Editor  { DataContext = new Pokedex4EditorViewModel(s) },                "Pokédex Editor (Gen 4)"),
            _           => (new PokedexSimpleEditor { DataContext = new PokedexSimpleEditorViewModel(CurrentSave) }, "Pokédex Editor (Simple)"),
        };

        await _dialogService.ShowDialogAsync(view, title);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBattlePassAsync()
    {
        if (CurrentSave is not SAV4BR sav) return;
        await _dialogService.ShowDialogAsync(
            new BattlePassEditor { DataContext = new BattlePassEditorViewModel(sav) },
            "Battle Pass Editor (PBR)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenUnderground8bAsync()
    {
        if (CurrentSave is not SAV8BS sav) return;
        await _dialogService.ShowDialogAsync(
            new Underground8bEditor { DataContext = new Underground8bEditorViewModel(sav) },
            "Underground Editor (BDSP)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSealStickers8bAsync()
    {
        if (CurrentSave is not SAV8BS sav) return;
        await _dialogService.ShowDialogAsync(
            new SealStickers8bEditor { DataContext = new SealStickers8bEditorViewModel(sav) },
            "Seal Stickers Editor (BDSP)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPoffin8bAsync()
    {
        if (CurrentSave is not SAV8BS sav) return;
        await _dialogService.ShowDialogAsync(
            new Poffin8bEditor { DataContext = new Poffin8bEditorViewModel(sav) },
            "Poffin Editor (BDSP)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenLink6Async()
    {
        if (CurrentSave is not SAV6 sav) return;
        await _dialogService.ShowDialogAsync(
            new Link6Editor { DataContext = new Link6EditorViewModel(sav, _dialogService) },
            "Pokémon Link Editor (Gen 6)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSimpleTrainerAsync()
    {
        if (CurrentSave is not (SAV1 or SAV2 or SAV3 or SAV4 or SAV5)) return;
        await _dialogService.ShowDialogAsync(
            new SimpleTrainerEditor { DataContext = new SimpleTrainerEditorViewModel(CurrentSave!) },
            "Simple Trainer Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenGearBRAsync()
    {
        if (CurrentSave is not SAV4BR sav) return;
        await _dialogService.ShowDialogAsync(
            new GearBREditor { DataContext = new GearBREditorViewModel(sav) },
            "Gear Editor (Battle Revolution)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSecretBase3Async()
    {
        if (CurrentSave is not SAV3 sav3hoenn || sav3hoenn is not (SAV3RS or SAV3E)) return;
        await _dialogService.ShowDialogAsync(
            new SecretBase3Editor { DataContext = new SecretBase3EditorViewModel(sav3hoenn) },
            "Secret Base Editor (RSE)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSecretBase6Async()
    {
        if (CurrentSave is not SAV6AO sav) return;
        await _dialogService.ShowDialogAsync(
            new SecretBase6Editor { DataContext = new SecretBase6EditorViewModel(sav) },
            "Secret Base Editor (ORAS)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokepuffAsync()
    {
        if (CurrentSave is not ISaveBlock6Main sav) return;
        await _dialogService.ShowDialogAsync(
            new PokepuffEditor { DataContext = new PokepuffEditorViewModel(sav) },
            "Poké Puff Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokeBlockAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new PokeBlockEditor { DataContext = new PokeBlockEditorViewModel(CurrentSave) },
            "Pokéblock Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBerryFieldAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new BerryFieldEditorView { DataContext = new BerryFieldEditorViewModel(CurrentSave) },
            "Berry Field Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRoamer6Async()
    {
        if (CurrentSave is not SAV6XY sav) return;
        await _dialogService.ShowDialogAsync(
            new Roamer6Editor { DataContext = new Roamer6EditorViewModel(sav) },
            "Roamer Editor (XY)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenChatterAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new ChatterEditor { DataContext = new ChatterEditorViewModel(CurrentSave) },
            "Chatter Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRTCAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new RTCEditor { DataContext = new RTCEditorViewModel(CurrentSave) },
            "RTC Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMedalAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new MedalEditorView { DataContext = new MedalEditorViewModel(CurrentSave) },
            "Medal Rally Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenUnityTower5Async()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new UnityTower5Editor { DataContext = new UnityTower5EditorViewModel(CurrentSave) },
            "Unity Tower Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPoffinCaseAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new PoffinCaseEditorView { DataContext = new PoffinCaseEditorViewModel(CurrentSave) },
            "Poffin Case Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPoketchAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new PoketchEditorView { DataContext = new PoketchEditorViewModel(CurrentSave) },
            "Pokétch Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokeBlock3CaseAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new PokeBlock3CaseEditorView { DataContext = new PokeBlock3CaseEditorViewModel(CurrentSave) },
            "Pokéblock Case Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHallOfFame3Async()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new HallOfFame3EditorView { DataContext = new HallOfFame3EditorViewModel(CurrentSave) },
            "Hall of Fame (Gen 3)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenFashionAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new FashionEditorView { DataContext = new FashionEditorViewModel(CurrentSave) },
            "Fashion Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenTrainerCard8Async()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new TrainerCard8EditorView { DataContext = new TrainerCard8EditorViewModel(CurrentSave) },
            "Trainer Card Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMailBoxAsync()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new MailBoxEditor { DataContext = new MailBoxEditorViewModel(CurrentSave) },
            "Mail Box Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenEventFlags2Async()
    {
        if (CurrentSave is not SAV2 sav) return;
        await _dialogService.ShowDialogAsync(
            new EventFlags2Editor { DataContext = new EventFlags2EditorViewModel(sav) },
            "Event Flags (Gen 2)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc2Async()
    {
        if (CurrentSave is not SAV2 sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc2Editor { DataContext = new Misc2EditorViewModel(sav) },
            "Misc Editor (Gen 2)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc3Async()
    {
        if (CurrentSave is not SAV3 sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc3Editor { DataContext = new Misc3EditorViewModel(sav) },
            "Misc Editor (Gen 3)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc4Async()
    {
        if (CurrentSave is not SAV4 sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc4Editor { DataContext = new Misc4EditorViewModel(sav) },
            "Misc Editor (Gen 4)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc5Async()
    {
        if (CurrentSave is not SAV5 sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc5Editor { DataContext = new Misc5EditorViewModel(sav) },
            "Misc Editor (Gen 5)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc7Async()
    {
        if (CurrentSave is not SAV7 sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc7Editor { DataContext = new Misc7EditorViewModel(sav) },
            "Misc Editor (Gen 7)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc8Async()
    {
        if (CurrentSave is not SAV8SWSH sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc8Editor { DataContext = new Misc8EditorViewModel(sav) },
            "Misc Editor (SWSH)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc9Async()
    {
        if (CurrentSave is not SAV9SV sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc9Editor { DataContext = new Misc9EditorViewModel(sav) },
            "Misc Editor (SV)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc7bAsync()
    {
        if (CurrentSave is not SAV7b sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc7bEditor { DataContext = new Misc7bEditorViewModel(sav) },
            "Misc Editor (Let's Go)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc8aAsync()
    {
        if (CurrentSave is not SAV8LA sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc8aEditor { DataContext = new Misc8aEditorViewModel(sav) },
            "Misc Editor (Legends Arceus)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc8bAsync()
    {
        if (CurrentSave is not SAV8BS sav) return;
        await _dialogService.ShowDialogAsync(
            new Misc8bEditor { DataContext = new Misc8bEditorViewModel(sav) },
            "Misc Editor (BDSP)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenEventReset1Async()
    {
        if (CurrentSave is not SAV1 sav) return;
        await _dialogService.ShowDialogAsync(
            new EventReset1Editor { DataContext = new EventReset1EditorViewModel(sav) },
            "Event Reset (Gen 1)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHallOfFame1Async()
    {
        if (CurrentSave is not SAV1 sav) return;
        await _dialogService.ShowDialogAsync(
            new HallOfFame1Editor { DataContext = new HallOfFame1EditorViewModel(sav) },
            "Hall of Fame (Gen 1)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRoamer3Async()
    {
        if (CurrentSave is not SAV3 sav) return;
        await _dialogService.ShowDialogAsync(
            new Roamer3Editor { DataContext = new Roamer3EditorViewModel(sav) },
            "Roamer Editor (Gen 3)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRTC3Async()
    {
        if (CurrentSave is not (SAV3RS or SAV3E)) return;
        await _dialogService.ShowDialogAsync(
            new RTC3Editor { DataContext = new RTC3EditorViewModel(CurrentSave!) },
            "RTC Editor (Gen 3)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenCapture7GGAsync()
    {
        if (CurrentSave is not SAV7b sav) return;
        await _dialogService.ShowDialogAsync(
            new Capture7GGEditor { DataContext = new Capture7GGEditorViewModel(sav) },
            "Capture Records (Let's Go)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHallOfFame7Async()
    {
        if (CurrentSave is null) return;
        await _dialogService.ShowDialogAsync(
            new HallOfFame7Editor { DataContext = new HallOfFame7EditorViewModel(CurrentSave) },
            "Hall of Fame (SM/USUM)");
    }
}
