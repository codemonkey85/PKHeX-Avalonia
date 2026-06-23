using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Presentation.ViewModels;

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
        await _windowService.ShowDialogAsync(new AboutViewModel(), "About PKHeX");
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var vm = new SettingsViewModel(_settings);
        await _windowService.ShowDialogAsync(vm, "Settings");

        // The sprite preference may have changed; re-apply the style and refresh open views.
        if (CurrentSave is not null)
        {
            _spriteRenderer.Initialize(CurrentSave);
            BoxViewer?.RefreshCurrentBox();
            PartyViewer?.RefreshParty();
            CurrentPokemonEditor?.RefreshSprite();
        }
    }

    [RelayCommand]
    private async Task OpenFolderListAsync()
    {
        var vm = new FolderListViewModel(_saveFileService, _settings, _dialogService);
        await _windowService.ShowDialogAsync(vm, "Save Folder List");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBatchEditorAsync()
    {
        if (CurrentSave is null) return;
        var vm = new BatchEditorViewModel(CurrentSave, _dialogService);
        vm.BatchEditCompleted += OnBatchEditCompleted;
        await _windowService.ShowDialogAsync(vm, "Batch Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBlockEditorAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new BlockEditorViewModel(CurrentSave, _dialogService),
            "Block Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBoxManipAsync()
    {
        if (CurrentSave is null) return;
        var vm = new BoxManipViewModel(CurrentSave, _dialogService, () => BoxViewer?.RefreshCurrentBox());
        await _windowService.ShowDialogAsync(vm, "Box Manipulation");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenEncounterDatabaseAsync()
    {
        if (CurrentSave is null) return;
        var vm = new EncounterDatabaseViewModel(CurrentSave, _spriteRenderer, _dialogService,
            pk => CurrentPokemonEditor?.LoadPKM(pk));
        await _windowService.ShowDialogAsync(vm, "Encounter Database");
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
        await _windowService.ShowDialogAsync(vm, "Group Viewer");
    }

    // — Save-specific editors —

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenDaycareAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new DaycareEditorViewModel(CurrentSave, _spriteRenderer),
            "Daycare");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRecordsAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new RecordsEditorViewModel(CurrentSave),
            "Game Records");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHallOfFameAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new HallOfFameEditorViewModel(CurrentSave, _spriteRenderer),
            "Hall of Fame");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSecretBaseAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new SecretBaseEditorViewModel(CurrentSave, _spriteRenderer),
            "Secret Base Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokebeanAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new PokebeanEditorViewModel(CurrentSave),
            "Poké Bean Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenFestivalPlazaAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new FestivalPlazaEditorViewModel(CurrentSave),
            "Festival Plaza Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRaidEditorAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new RaidEditorViewModel(CurrentSave),
            "Raid Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSuperTrainingAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new SuperTrainingEditorViewModel(CurrentSave),
            "Super Training Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenApricornAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new ApricornEditorViewModel(CurrentSave),
            "Apricorn Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokeGear4Async()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new PokeGear4EditorViewModel(CurrentSave),
            "PokéGear Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenGeonet4Async()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new Geonet4EditorViewModel(CurrentSave),
            "Geonet Globe Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBoxLayoutAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new BoxLayoutEditorViewModel(CurrentSave),
            "Box Layout Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHoneyTreeAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new HoneyTreeEditorViewModel(CurrentSave),
            "Honey Tree Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenUndergroundAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new UndergroundEditorViewModel(CurrentSave),
            "Underground Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRoamerAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new RoamerEditorViewModel(CurrentSave),
            "Roamer Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenOPowerAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new OPowerEditorViewModel(CurrentSave),
            "O-Power Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenZygardeCellAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new ZygardeCellEditorViewModel(CurrentSave),
            "Zygarde Cell Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRaid9Async()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new Raid9EditorViewModel(CurrentSave),
            "Tera Raid Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRaidSevenStar9Async()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new RaidSevenStar9EditorViewModel(CurrentSave),
            "7-Star Tera Raid Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenFashion9Async()
    {
        if (CurrentSave is not (SAV9SV or SAV9ZA)) return;
        await _windowService.ShowDialogAsync(
            new Fashion9EditorViewModel(CurrentSave!),
            "Fashion Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenDonutAsync()
    {
        if (CurrentSave is not SAV9ZA sav) return;
        await _windowService.ShowDialogAsync(
            new DonutEditorViewModel(sav),
            "Donut Editor (PLZA)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenDLC5Async()
    {
        if (CurrentSave is not SAV5 sav) return;
        await _windowService.ShowDialogAsync(
            new DLC5EditorViewModel(sav, _dialogService),
            "DLC Editor (Gen 5)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenEntralinkAsync()
    {
        if (CurrentSave is not SAV5 sav) return;
        await _windowService.ShowDialogAsync(
            new EntralinkEditorViewModel(sav),
            "Entralink Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokedexAsync()
    {
        if (CurrentSave is null) return;

        (object view, string title) = CurrentSave switch
        {
            SAV9SV s    => ((object)new PokedexGen9EditorViewModel(s),   "Pokédex Editor (Gen 9 SV)"),
            SAV8SWSH s  => (new Pokedex8EditorViewModel(s),                "Pokédex Editor (Gen 8 SwSh)"),
            SAV8BS s    => (new Pokedex8bEditorViewModel(s),               "Pokédex Editor (Gen 8 BDSP)"),
            SAV8LA s    => (new PokedexLAEditorViewModel(s),               "Pokédex Editor (PLA)"),
            SAV7b s     => (new Pokedex7bEditorViewModel(s),               "Pokédex Editor (Let's Go)"),
            SAV7 s      => (new Pokedex7EditorViewModel(s),                "Pokédex Editor (Gen 7)"),
            SAV6 s      => (new Pokedex6EditorViewModel(s),                "Pokédex Editor (Gen 6)"),
            SAV5 s      => (new Pokedex5EditorViewModel(s),                "Pokédex Editor (Gen 5)"),
            SAV4 s      => (new Pokedex4EditorViewModel(s),                "Pokédex Editor (Gen 4)"),
            _           => (new PokedexSimpleEditorViewModel(CurrentSave), "Pokédex Editor (Simple)"),
        };

        await _windowService.ShowDialogAsync(view, title);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBattlePassAsync()
    {
        if (CurrentSave is not SAV4BR sav) return;
        await _windowService.ShowDialogAsync(
            new BattlePassEditorViewModel(sav),
            "Battle Pass Editor (PBR)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenUnderground8bAsync()
    {
        if (CurrentSave is not SAV8BS sav) return;
        await _windowService.ShowDialogAsync(
            new Underground8bEditorViewModel(sav),
            "Underground Editor (BDSP)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSealStickers8bAsync()
    {
        if (CurrentSave is not SAV8BS sav) return;
        await _windowService.ShowDialogAsync(
            new SealStickers8bEditorViewModel(sav),
            "Seal Stickers Editor (BDSP)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPoffin8bAsync()
    {
        if (CurrentSave is not SAV8BS sav) return;
        await _windowService.ShowDialogAsync(
            new Poffin8bEditorViewModel(sav),
            "Poffin Editor (BDSP)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenLink6Async()
    {
        if (CurrentSave is not SAV6 sav) return;
        await _windowService.ShowDialogAsync(
            new Link6EditorViewModel(sav, _dialogService),
            "Pokémon Link Editor (Gen 6)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSimpleTrainerAsync()
    {
        if (CurrentSave is not (SAV1 or SAV2 or SAV3 or SAV4 or SAV5)) return;
        await _windowService.ShowDialogAsync(
            new SimpleTrainerEditorViewModel(CurrentSave!),
            "Simple Trainer Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenGearBRAsync()
    {
        if (CurrentSave is not SAV4BR sav) return;
        await _windowService.ShowDialogAsync(
            new GearBREditorViewModel(sav),
            "Gear Editor (Battle Revolution)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSecretBase3Async()
    {
        if (CurrentSave is not SAV3 sav3hoenn || sav3hoenn is not (SAV3RS or SAV3E)) return;
        await _windowService.ShowDialogAsync(
            new SecretBase3EditorViewModel(sav3hoenn),
            "Secret Base Editor (RSE)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenSecretBase6Async()
    {
        if (CurrentSave is not SAV6AO sav) return;
        await _windowService.ShowDialogAsync(
            new SecretBase6EditorViewModel(sav),
            "Secret Base Editor (ORAS)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokepuffAsync()
    {
        if (CurrentSave is not ISaveBlock6Main sav) return;
        await _windowService.ShowDialogAsync(
            new PokepuffEditorViewModel(sav),
            "Poké Puff Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokeBlockAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new PokeBlockEditorViewModel(CurrentSave),
            "Pokéblock Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenBerryFieldAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new BerryFieldEditorViewModel(CurrentSave),
            "Berry Field Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRoamer6Async()
    {
        if (CurrentSave is not SAV6XY sav) return;
        await _windowService.ShowDialogAsync(
            new Roamer6EditorViewModel(sav),
            "Roamer Editor (XY)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenChatterAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new ChatterEditorViewModel(CurrentSave),
            "Chatter Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRTCAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new RTCEditorViewModel(CurrentSave),
            "RTC Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMedalAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new MedalEditorViewModel(CurrentSave, _dialogService),
            "Medal Rally Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenUnityTower5Async()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new UnityTower5EditorViewModel(CurrentSave),
            "Unity Tower Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPoffinCaseAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new PoffinCaseEditorViewModel(CurrentSave),
            "Poffin Case Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPoketchAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new PoketchEditorViewModel(CurrentSave),
            "Pokétch Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPokeBlock3CaseAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new PokeBlock3CaseEditorViewModel(CurrentSave),
            "Pokéblock Case Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHallOfFame3Async()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new HallOfFame3EditorViewModel(CurrentSave),
            "Hall of Fame (Gen 3)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenFashionAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new FashionEditorViewModel(CurrentSave),
            "Fashion Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenTrainerCard8Async()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new TrainerCard8EditorViewModel(CurrentSave),
            "Trainer Card Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMailBoxAsync()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new MailBoxEditorViewModel(CurrentSave),
            "Mail Box Editor");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenEventFlags2Async()
    {
        if (CurrentSave is not SAV2 sav) return;
        await _windowService.ShowDialogAsync(
            new EventFlags2EditorViewModel(sav),
            "Event Flags (Gen 2)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc2Async()
    {
        if (CurrentSave is not SAV2 sav) return;
        await _windowService.ShowDialogAsync(
            new Misc2EditorViewModel(sav),
            "Misc Editor (Gen 2)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc3Async()
    {
        if (CurrentSave is not SAV3 sav) return;
        await _windowService.ShowDialogAsync(
            new Misc3EditorViewModel(sav),
            "Misc Editor (Gen 3)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc4Async()
    {
        if (CurrentSave is not SAV4 sav) return;
        await _windowService.ShowDialogAsync(
            new Misc4EditorViewModel(sav),
            "Misc Editor (Gen 4)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc5Async()
    {
        if (CurrentSave is not SAV5 sav) return;
        await _windowService.ShowDialogAsync(
            new Misc5EditorViewModel(sav),
            "Misc Editor (Gen 5)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc7Async()
    {
        if (CurrentSave is not SAV7 sav) return;
        await _windowService.ShowDialogAsync(
            new Misc7EditorViewModel(sav),
            "Misc Editor (Gen 7)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc8Async()
    {
        if (CurrentSave is not SAV8SWSH sav) return;
        await _windowService.ShowDialogAsync(
            new Misc8EditorViewModel(sav),
            "Misc Editor (SWSH)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc9Async()
    {
        if (CurrentSave is not SAV9SV sav) return;
        await _windowService.ShowDialogAsync(
            new Misc9EditorViewModel(sav),
            "Misc Editor (SV)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc7bAsync()
    {
        if (CurrentSave is not SAV7b sav) return;
        await _windowService.ShowDialogAsync(
            new Misc7bEditorViewModel(sav),
            "Misc Editor (Let's Go)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc8aAsync()
    {
        if (CurrentSave is not SAV8LA sav) return;
        await _windowService.ShowDialogAsync(
            new Misc8aEditorViewModel(sav),
            "Misc Editor (Legends Arceus)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMisc8bAsync()
    {
        if (CurrentSave is not SAV8BS sav) return;
        await _windowService.ShowDialogAsync(
            new Misc8bEditorViewModel(sav),
            "Misc Editor (BDSP)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenEventReset1Async()
    {
        if (CurrentSave is not SAV1 sav) return;
        await _windowService.ShowDialogAsync(
            new EventReset1EditorViewModel(sav),
            "Event Reset (Gen 1)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHallOfFame1Async()
    {
        if (CurrentSave is not SAV1 sav) return;
        await _windowService.ShowDialogAsync(
            new HallOfFame1EditorViewModel(sav),
            "Hall of Fame (Gen 1)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRoamer3Async()
    {
        if (CurrentSave is not SAV3 sav) return;
        await _windowService.ShowDialogAsync(
            new Roamer3EditorViewModel(sav),
            "Roamer Editor (Gen 3)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenRTC3Async()
    {
        if (CurrentSave is not (SAV3RS or SAV3E)) return;
        await _windowService.ShowDialogAsync(
            new RTC3EditorViewModel(CurrentSave!),
            "RTC Editor (Gen 3)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenCapture7GGAsync()
    {
        if (CurrentSave is not SAV7b sav) return;
        await _windowService.ShowDialogAsync(
            new Capture7GGEditorViewModel(sav),
            "Capture Records (Let's Go)");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenHallOfFame7Async()
    {
        if (CurrentSave is null) return;
        await _windowService.ShowDialogAsync(
            new HallOfFame7EditorViewModel(CurrentSave),
            "Hall of Fame (SM/USUM)");
    }
}
