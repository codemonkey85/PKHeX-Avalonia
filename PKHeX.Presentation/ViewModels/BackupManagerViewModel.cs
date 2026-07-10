using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Application.Abstractions;
using PKHeX.Application.Services;
using PKHeX.Core;

namespace PKHeX.Presentation.ViewModels;

/// <summary>
/// Backup Manager: lists the timestamped backups (issue #135) kept for the currently open save,
/// and lets the user restore, delete, or reveal one in the OS file manager. Shown as a modeless
/// tool window via <see cref="IWindowService.ShowTool"/>.
/// </summary>
public partial class BackupManagerViewModel : ViewModelBase
{
    private readonly ISaveBackupService _backupService;
    private readonly ISaveFileGateway _saveFileService;
    private readonly IDialogService _dialogService;
    private readonly AppSettings _settings;

    [ObservableProperty]
    private ObservableCollection<BackupEntryRow> _backups = [];

    [ObservableProperty]
    private BackupEntryRow? _selectedBackup;

    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>Raised after a successful restore so the host can refresh any other open editors/tools.</summary>
    public event Action? Restored;

    public BackupManagerViewModel(ISaveBackupService backupService, ISaveFileGateway saveFileService, IDialogService dialogService, AppSettings settings)
    {
        _backupService = backupService;
        _saveFileService = saveFileService;
        _dialogService = dialogService;
        _settings = settings;
        Refresh();
    }

    [RelayCommand]
    public void Refresh()
    {
        var path = _saveFileService.CurrentPath;
        if (string.IsNullOrEmpty(path))
        {
            Backups = [];
            StatusText = "Save this file at least once to enable backups.";
            return;
        }

        var identity = SaveIdentity.Compute(path);
        var result = _backupService.ListBackups(identity);

        Backups = new ObservableCollection<BackupEntryRow>(result.Entries.Select(e => new BackupEntryRow(e)));
        StatusText = result.Warnings.Count == 0
            ? $"{Backups.Count} backup(s)"
            : $"{Backups.Count} backup(s) — {result.Warnings.Count} skipped (corrupt/unreadable): {string.Join("; ", result.Warnings)}";
    }

    [RelayCommand]
    private async Task RestoreSelectedAsync()
    {
        if (SelectedBackup is null)
            return;

        var currentPath = _saveFileService.CurrentPath;
        if (string.IsNullOrEmpty(currentPath))
        {
            await _dialogService.ShowErrorAsync("Restore Failed", "No save file is currently open at a known path.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Restore Backup",
            $"Restore the backup from {SelectedBackup.Timestamp}?\n\nThe current save state will itself be backed up first, then overwritten.");
        if (!confirmed)
            return;

        byte[] backupData;
        try
        {
            backupData = _backupService.ReadBackup(SelectedBackup.Entry);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Restore Failed", $"This backup could not be read. It may be corrupt.\n\n{ex.Message}");
            Refresh();
            return;
        }

        // Validate the backup actually parses as a save before touching anything on disk.
        // (Not just "didn't throw" — FileUtil also recognizes PKM/box-binary/etc. formats, so make
        // sure what comes back is specifically a SaveFile.)
        SaveFile? restoredSave;
        try
        {
            restoredSave = FileUtil.GetSupportedFile(backupData, Path.GetFileName(currentPath)) as SaveFile;
        }
        catch
        {
            restoredSave = null;
        }

        if (restoredSave is null)
        {
            await _dialogService.ShowErrorAsync("Restore Failed", "This backup is corrupt and cannot be restored.");
            Refresh();
            return;
        }

        // Pre-restore backup of the current on-disk state, so restoring is itself undoable.
        if (File.Exists(currentPath))
        {
            try
            {
                var identity = SaveIdentity.Compute(currentPath);
                var existing = File.ReadAllBytes(currentPath);
                _backupService.CreateBackup(identity, existing, _settings.SaveBackup.MaxBackupsPerSave);
            }
            catch
            {
                // Non-fatal — proceed with the restore even if the pre-restore snapshot failed.
            }
        }

        File.WriteAllBytes(currentPath, backupData);
        var reloaded = await _saveFileService.LoadSaveFileAsync(currentPath);
        if (!reloaded)
        {
            await _dialogService.ShowErrorAsync("Restore Failed", "The backup was written to disk, but the app could not reload it.");
            return;
        }

        Refresh();
        Restored?.Invoke();
        await _dialogService.ShowInformationAsync("Restore Complete", "The backup has been restored and reloaded.");
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedBackup is null)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Backup",
            $"Delete the backup from {SelectedBackup.Timestamp}? This cannot be undone.");
        if (!confirmed)
            return;

        _backupService.DeleteBackup(SelectedBackup.Entry);
        Refresh();
    }

    [RelayCommand]
    private void RevealSelected()
    {
        if (SelectedBackup is null)
            return;
        _dialogService.RevealInFileManager(SelectedBackup.FilePath);
    }
}
