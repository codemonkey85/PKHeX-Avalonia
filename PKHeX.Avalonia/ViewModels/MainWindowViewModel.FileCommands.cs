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

        int count = 0;
        for (int b = 0; b < CurrentSave.BoxCount; b++)
        {
            var boxData = CurrentSave.GetBoxData(b);
            for (int s = 0; s < boxData.Length; s++)
            {
                var pk = boxData[s];
                if (pk.Species == 0) continue;

                var fileName = $"{b + 1:00}_{s + 1:00} - {pk.Nickname} - {pk.PID:X8}.{pk.Extension}";
                foreach (var c in Path.GetInvalidFileNameChars())
                    fileName = fileName.Replace(c, '_');

                File.WriteAllBytes(Path.Combine(path, fileName), pk.Data);
                count++;
            }
        }

        await _dialogService.ShowInformationAsync("Dump Boxes", $"Dumped {count} Pokémon to {path}");
    }

    [RelayCommand(CanExecute = nameof(HasSave))]
    private async Task LoadBoxesAsync()
    {
        if (CurrentSave is null) return;

        var path = await _dialogService.OpenFolderAsync("Select Folder to Load Boxes");
        if (string.IsNullOrEmpty(path)) return;

        var extensions = EntityFileExtension.GetExtensions()
            .Select(e => "." + e)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f)))
            .ToList();

        if (files.Count == 0)
        {
            await _dialogService.ShowInformationAsync("Load Boxes", "No supported Pokémon files found in the selected folder.");
            return;
        }

        int loaded = 0, skipped = 0, currentBox = 0, currentSlot = 0;

        foreach (var file in files)
        {
            try
            {
                var pk = EntityFormat.GetFromBytes(File.ReadAllBytes(file), CurrentSave.Context);
                if (pk is null) { skipped++; continue; }

                bool placed = false;
                while (currentBox < CurrentSave.BoxCount)
                {
                    while (currentSlot < CurrentSave.BoxSlotCount)
                    {
                        if (CurrentSave.GetBoxSlotAtIndex(currentBox, currentSlot).Species == 0)
                        {
                            CurrentSave.SetBoxSlotAtIndex(pk, currentBox, currentSlot);
                            placed = true;
                            loaded++;
                            break;
                        }
                        currentSlot++;
                    }
                    if (placed) break;
                    currentSlot = 0;
                    currentBox++;
                }

                if (!placed) break;
            }
            catch
            {
                skipped++;
            }
        }

        BoxViewer?.RefreshCurrentBox();

        var message = $"Loaded {loaded} Pokémon.";
        if (skipped > 0) message += $" Skipped {skipped} files.";
        if (currentBox >= CurrentSave.BoxCount && loaded < files.Count) message += " Stopped — boxes are full.";

        await _dialogService.ShowInformationAsync("Load Boxes", message);
    }
}
