using CommunityToolkit.Mvvm.Input;
using PKHeX.Avalonia.Views;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var path = await _dialogService.OpenFileAsync(
            "Open Save File",
            ["*.sav", "*.bin", "main", "*"]);

        if (string.IsNullOrEmpty(path))
            return;

        var success = await _saveFileService.LoadSaveFileAsync(path);
        if (!success)
            await _dialogService.ShowErrorAsync("Error", $"Could not load the save file at:\n{path}");
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
        if (string.IsNullOrWhiteSpace(text))
        {
            await _dialogService.ShowErrorAsync("Import Failed", "Clipboard is empty.");
            return;
        }

        var set = new ShowdownSet(text.Trim());
        if (set.Species <= 0)
        {
            await _dialogService.ShowErrorAsync("Import Failed", "Invalid Showdown set text.");
            return;
        }

        var pk = CurrentSave.BlankPKM;
        pk.ApplySetDetails(set);
        if (pk.Format >= 8)
            pk.Nature = pk.StatNature;
        pk.SetPIDGender(pk.Gender);

        CurrentPokemonEditor.LoadPKM(pk);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task ExportShowdownAsync()
    {
        if (CurrentPokemonEditor is null) return;

        var pk = CurrentPokemonEditor.PreparePKM();
        if (pk.Species == 0) return;

        await _clipboardService.SetTextAsync(new ShowdownSet(pk).Text);
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenPKMDatabaseAsync()
    {
        if (CurrentSave is null) return;

        var vm = new PKMDatabaseViewModel(CurrentSave, _spriteRenderer, _dialogService);
        vm.PokemonSelected += pk => CurrentPokemonEditor?.LoadPKM(pk);

        await _dialogService.ShowDialogAsync(new PKMDatabaseView { DataContext = vm }, "PKM Database");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task OpenMysteryGiftDatabaseAsync()
    {
        if (CurrentSave is null) return;

        var vm = new MysteryGiftDatabaseViewModel(CurrentSave, _spriteRenderer, _dialogService);
        vm.GiftSelected += mg => CurrentPokemonEditor?.LoadPKM(mg.ConvertToPKM(CurrentSave));

        await _dialogService.ShowDialogAsync(new MysteryGiftDatabaseView { DataContext = vm }, "Mystery Gift Database");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task DumpBoxesAsync()
    {
        if (CurrentSave is null) return;

        var path = await _dialogService.OpenFolderAsync("Select Folder to Dump Boxes");
        if (string.IsNullOrEmpty(path)) return;

        // Core's DumpBoxes writes proper .pk* files (decrypted party format with
        // OT/nickname blocks for Gen 1/2) that can be re-imported on any save.
        int count = CurrentSave.DumpBoxes(path);
        if (count < 0)
        {
            await _dialogService.ShowErrorAsync("Dump Boxes", "This save file has no boxes to dump.");
            return;
        }

        await _dialogService.ShowInformationAsync("Dump Boxes", $"Dumped {count} Pokémon to {path}");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task LoadBoxesAsync()
    {
        if (CurrentSave is null) return;

        var path = await _dialogService.OpenFolderAsync("Select Folder to Load Boxes");
        if (string.IsNullOrEmpty(path)) return;

        // Core's LoadBoxes handles file detection, format conversion between
        // games/generations (e.g. Gold -> Crystal) and compatibility checks.
        int count = CurrentSave.LoadBoxes(path, out var result, all: true);

        BoxViewer?.RefreshCurrentBox();

        if (count < 0)
            await _dialogService.ShowErrorAsync("Load Boxes", result);
        else
            await _dialogService.ShowInformationAsync("Load Boxes", result);
    }
}
