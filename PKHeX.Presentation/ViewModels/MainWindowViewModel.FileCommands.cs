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
    private async Task OpenPKMDatabaseAsync()
    {
        if (CurrentSave is null) return;

        var vm = new PKMDatabaseViewModel(CurrentSave, _spriteRenderer, _dialogService);
        vm.PokemonSelected += pk => CurrentPokemonEditor?.LoadPKM(pk);

        await _windowService.ShowDialogAsync(vm, "PKM Database");
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
