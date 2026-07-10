using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Presentation.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var path = await _dialogService.OpenFileAsync(
            "Open Save File",
            FileDialogFilters.OpenSaveFile);

        if (string.IsNullOrEmpty(path))
            return;

        await OpenSaveFilePathAsync(path);
    }

    private async Task OpenSaveFilePathAsync(string path)
    {
        var success = await _saveFileService.LoadSaveFileAsync(path);
        if (!success)
            await _dialogService.ShowErrorAsync("Error", $"Could not load the save file at:\n{path}");
    }

    // Fired by BoxViewer/PartyViewer when an OS file dropped onto a slot turns out to be a save
    // file rather than a Pokémon entity; routed through the same "open save" path as File > Open.
    private void OnSaveFileDropRequested(string path) => _ = OpenSaveFilePathAsync(path);

    /// <summary>
    /// Handles one or more OS files dropped anywhere on the main window. A save file is opened
    /// (same path as File &gt; Open); Pokémon entity files are loaded into the current editor
    /// without writing to a slot (drops onto a specific box/party slot are handled separately by
    /// <see cref="BoxViewerViewModel.HandleFileDropAsync"/>/<see cref="PartyViewerViewModel.HandleFileDropAsync"/>).
    /// </summary>
    public async Task HandleWindowFileDropAsync(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
            return;

        // A dropped save file always wins, regardless of how many other files came with it.
        foreach (var path in paths)
        {
            var obj = TryGetSupportedFile(path);
            if (obj is SaveFile)
            {
                await OpenSaveFilePathAsync(path);
                return;
            }
        }

        if (CurrentSave is null || CurrentPokemonEditor is null)
            return;

        // Load the first recognizable entity into the editor; extras are ignored (the editor holds one Pokémon).
        foreach (var path in paths)
        {
            var result = new ImportEntityFileUseCase().Execute(CurrentSave, path);
            if (result.Kind == EntityFileDropKind.Entity)
            {
                CurrentPokemonEditor.LoadPKM(result.Entity!);
                return;
            }
        }

        await _dialogService.ShowErrorAsync("Import Failed", "No supported Pokémon or save file was found in the dropped file(s).");
    }

    private object? TryGetSupportedFile(string path)
    {
        try { return FileUtil.GetSupportedFile(path, CurrentSave); }
        catch { return null; }
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task SaveFileAsync()
    {
        var success = await _saveFileService.SaveFileAsync();
        if (!success)
            await _dialogService.ShowErrorAsync("Error", "Failed to save file.");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task SaveFileAsAsync()
    {
        var path = await _dialogService.SaveFileAsync("Save As", CurrentSave?.Metadata.FileName);
        if (string.IsNullOrEmpty(path))
            return;

        var success = await _saveFileService.SaveFileAsync(path);
        if (!success)
            await _dialogService.ShowErrorAsync("Error", "Failed to save file.");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private void CloseFile() => _saveFileService.CloseSave();

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task ImportShowdownAsync()
    {
        if (CurrentSave is null || CurrentPokemonEditor is null) return;

        var text = await _clipboardService.GetTextAsync();
        var result = new ImportShowdownSetUseCase().Execute(CurrentSave, text);
        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync("Import Failed", result.Error!);
            return;
        }

        CurrentPokemonEditor.LoadPKM(result.Pokemon!);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task ExportShowdownAsync()
    {
        if (CurrentPokemonEditor is null) return;

        var text = new ExportShowdownSetUseCase().Execute(CurrentPokemonEditor.PreparePKM());
        if (text is null) return;

        await _clipboardService.SetTextAsync(text);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task ShowQrCodeAsync()
    {
        if (CurrentSave is null || CurrentPokemonEditor is null) return;

        var pk = CurrentPokemonEditor.PreparePKM();
        if (pk.Species == 0)
        {
            await _dialogService.ShowErrorAsync("QR Code", "Load a Pokémon into the editor first.");
            return;
        }

        var message = QRMessageUtil.GetMessage(pk);
        var png = _qrCodeService.GeneratePng(message);
        var species = GameInfo.Strings.Species[pk.Species];
        var caption = pk.Format == 7
            ? $"{species} — scannable by the Gen 7 in-game QR Scanner."
            : $"{species} — raw data QR (import via PKHeX's QR import).";

        await _windowService.ShowDialogAsync(
            new QrCodeViewModel(png, caption, $"{species}_qr.png", _dialogService),
            "QR Code");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task ImportQrCodeAsync()
    {
        if (CurrentSave is null || CurrentPokemonEditor is null) return;

        var path = await _dialogService.OpenFileAsync("Open QR Code Image", ["*.png", "*.jpg", "*.jpeg", "*.bmp"]);
        if (string.IsNullOrEmpty(path)) return;

        string? message;
        try
        {
            message = _qrCodeService.DecodePng(File.ReadAllBytes(path));
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("QR Import Failed", $"Could not read the image.\n\n{ex.Message}");
            return;
        }

        if (message is null)
        {
            await _dialogService.ShowErrorAsync("QR Import Failed", "No QR code was found in the image.");
            return;
        }

        var pk = QRMessageUtil.GetPKM(message, CurrentSave.Context);
        if (pk is null)
        {
            await _dialogService.ShowErrorAsync("QR Import Failed",
                "The QR code was read, but it does not contain Pokémon data compatible with this save.");
            return;
        }

        CurrentPokemonEditor.LoadPKM(pk);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task ImportShowdownTeamAsync()
    {
        if (CurrentSave is null || BoxViewer is null) return;

        var text = await _clipboardService.GetTextAsync();
        var result = new ImportShowdownTeamUseCase().Execute(CurrentSave, text, BoxViewer.CurrentBox);
        if (!result.Success)
        {
            var detail = result.SetErrors.Count > 0
                ? $"{result.FatalError}\n\n{string.Join("\n", result.SetErrors)}"
                : result.FatalError!;
            await _dialogService.ShowErrorAsync("Import Team Failed", detail);
            return;
        }

        BoxViewer.RefreshCurrentBox();

        var message = $"Imported {result.Imported} Pokémon into the current box.";
        if (result.SetErrors.Count > 0)
            message += $"\n\nSkipped sets:\n{string.Join("\n", result.SetErrors)}";
        await _dialogService.ShowInformationAsync("Import Team", message);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task ExportShowdownBoxAsync()
    {
        if (CurrentSave is null || BoxViewer is null) return;

        var text = new ExportShowdownBoxUseCase().Execute(CurrentSave, BoxViewer.CurrentBox);
        if (text.Length == 0)
        {
            await _dialogService.ShowInformationAsync("Export Box", "The current box is empty.");
            return;
        }
        await _clipboardService.SetTextAsync(text);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task ExportShowdownAllBoxesAsync()
    {
        if (CurrentSave is null) return;

        var text = new ExportShowdownBoxUseCase().ExecuteAll(CurrentSave);
        if (text.Length == 0)
        {
            await _dialogService.ShowInformationAsync("Export All Boxes", "All boxes are empty.");
            return;
        }
        await _clipboardService.SetTextAsync(text);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPKMDatabaseAsync()
    {
        if (CurrentSave is null) return;

        var vm = new PKMDatabaseViewModel(CurrentSave, _spriteRenderer, _dialogService);
        vm.PokemonSelected += pk => CurrentPokemonEditor?.LoadPKM(pk);

        await _windowService.ShowDialogAsync(vm, "PKM Database");
    }

    // Cached so re-invoking the menu item focuses the existing tool window instead of
    // opening a duplicate (ShowTool keys off the ViewModel instance). Reset on save change.
    private BoxReportViewModel? _boxReport;

    [RelayCommand(CanExecute = nameof(HasSave))]
    private void OpenBoxReport()
    {
        if (CurrentSave is null) return;

        if (_boxReport is null)
        {
            _boxReport = new BoxReportViewModel(CurrentSave, _dialogService);
            _boxReport.RowActivated += row =>
            {
                ((IBoxNavigator?)BoxViewer)?.NavigateTo(row.Box, row.Slot);
                CurrentPokemonEditor?.LoadPKM(row.Entity);
            };
        }
        else
        {
            _boxReport.Refresh();
        }

        _windowService.ShowTool(_boxReport, "Box Data Report");
    }

    // Cached so re-invoking the menu item focuses the existing tool window instead of
    // opening a duplicate (ShowTool keys off the ViewModel instance). Reset on save change.
    private LegalityAuditViewModel? _legalityAudit;

    [RelayCommand(CanExecute = nameof(HasSave))]
    private void OpenLegalityAudit()
    {
        if (CurrentSave is null) return;

        if (_legalityAudit is null)
        {
            _legalityAudit = new LegalityAuditViewModel(CurrentSave, _dialogService);
            _legalityAudit.RowActivated += row =>
            {
                if (row.IsParty)
                    PartyViewer?.RefreshParty();
                else
                    ((IBoxNavigator?)BoxViewer)?.NavigateTo(row.Box, row.Slot);
                CurrentPokemonEditor?.LoadPKM(row.Entity);
            };
        }

        _windowService.ShowTool(_legalityAudit, "Legality Audit");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMysteryGiftDatabaseAsync()
    {
        if (CurrentSave is null) return;

        var vm = new MysteryGiftDatabaseViewModel(CurrentSave, _spriteRenderer, _dialogService);
        vm.GiftSelected += mg => CurrentPokemonEditor?.LoadPKM(mg.ConvertToPKM(CurrentSave));

        await _windowService.ShowDialogAsync(vm, "Mystery Gift Database");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task DumpBoxesAsync()
    {
        if (CurrentSave is null) return;

        var path = await _dialogService.OpenFolderAsync("Select Folder to Dump Boxes");
        if (string.IsNullOrEmpty(path)) return;

        var result = new DumpBoxesUseCase().Execute(CurrentSave, path);
        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync("Dump Boxes", result.Message);
            return;
        }

        await _dialogService.ShowInformationAsync("Dump Boxes", result.Message);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task LoadBoxesAsync()
    {
        if (CurrentSave is null) return;

        var path = await _dialogService.OpenFolderAsync("Select Folder to Load Boxes");
        if (string.IsNullOrEmpty(path)) return;

        var result = new LoadBoxesUseCase().Execute(CurrentSave, path);

        BoxViewer?.RefreshCurrentBox();

        if (!result.Success)
            await _dialogService.ShowErrorAsync("Load Boxes", result.Message);
        else
            await _dialogService.ShowInformationAsync("Load Boxes", result.Message);
    }
}
