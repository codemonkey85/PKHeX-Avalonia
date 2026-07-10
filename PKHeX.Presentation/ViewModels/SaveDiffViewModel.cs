using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Application.Abstractions;
using PKHeX.Application.Services;
using PKHeX.Application.UseCases;
using PKHeX.Core;

namespace PKHeX.Presentation.ViewModels;

/// <summary>
/// Save Comparison viewer (issue #135): diffs the currently open save against one of its backups,
/// or diffs two backups against each other. Shown as a modeless tool window.
/// </summary>
public partial class SaveDiffViewModel : ViewModelBase
{
    private readonly SaveFile _currentSave;
    private readonly string? _currentPath;
    private readonly ISaveBackupService _backupService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<BackupEntryRow> _availableBackups = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCurrentWithBackupCommand))]
    [NotifyCanExecuteChangedFor(nameof(CompareTwoBackupsCommand))]
    private BackupEntryRow? _selectedBackupA;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareTwoBackupsCommand))]
    private BackupEntryRow? _selectedBackupB;

    [ObservableProperty]
    private ObservableCollection<SaveDiffChangeRow> _changes = [];

    [ObservableProperty]
    private string _statusText = string.Empty;

    public SaveDiffViewModel(SaveFile currentSave, string? currentPath, ISaveBackupService backupService, IDialogService dialogService)
    {
        _currentSave = currentSave;
        _currentPath = currentPath;
        _backupService = backupService;
        _dialogService = dialogService;
        RefreshBackups();
    }

    [RelayCommand]
    public void RefreshBackups()
    {
        if (string.IsNullOrEmpty(_currentPath))
        {
            AvailableBackups = [];
            StatusText = "Save this file at least once to compare against backups.";
            return;
        }

        var identity = SaveIdentity.Compute(_currentPath);
        var result = _backupService.ListBackups(identity);
        AvailableBackups = new ObservableCollection<BackupEntryRow>(result.Entries.Select(e => new BackupEntryRow(e)));

        StatusText = result.Warnings.Count > 0
            ? $"{result.Warnings.Count} backup(s) skipped (corrupt/unreadable): {string.Join("; ", result.Warnings)}"
            : $"{AvailableBackups.Count} backup(s) available.";
    }

    [RelayCommand(CanExecute = nameof(CanCompareCurrentWithBackup))]
    private async Task CompareCurrentWithBackupAsync()
    {
        if (SelectedBackupA is null)
            return;

        if (!TryLoadBackup(SelectedBackupA, out var other, out var error))
        {
            await _dialogService.ShowErrorAsync("Compare Failed", error!);
            return;
        }

        RunDiff(_currentSave, other!, $"Current Save vs. Backup ({SelectedBackupA.Timestamp})");
    }

    private bool CanCompareCurrentWithBackup => SelectedBackupA is not null;

    [RelayCommand(CanExecute = nameof(CanCompareTwoBackups))]
    private async Task CompareTwoBackupsAsync()
    {
        if (SelectedBackupA is null || SelectedBackupB is null)
            return;

        if (!TryLoadBackup(SelectedBackupA, out var left, out var errorA))
        {
            await _dialogService.ShowErrorAsync("Compare Failed", errorA!);
            return;
        }
        if (!TryLoadBackup(SelectedBackupB, out var right, out var errorB))
        {
            await _dialogService.ShowErrorAsync("Compare Failed", errorB!);
            return;
        }

        RunDiff(left!, right!, $"Backup ({SelectedBackupA.Timestamp}) vs. Backup ({SelectedBackupB.Timestamp})");
    }

    private bool CanCompareTwoBackups => SelectedBackupA is not null && SelectedBackupB is not null;

    private bool TryLoadBackup(BackupEntryRow row, out SaveFile? save, out string? error)
    {
        save = null;
        try
        {
            var data = _backupService.ReadBackup(row.Entry);
            save = FileUtil.GetSupportedFile(data, row.FileName) as SaveFile;
        }
        catch (Exception ex)
        {
            error = $"The backup from {row.Timestamp} is corrupt and cannot be compared.\n\n{ex.Message}";
            return false;
        }

        if (save is null)
        {
            error = $"The backup from {row.Timestamp} is corrupt and cannot be compared.";
            return false;
        }

        error = null;
        return true;
    }

    private void RunDiff(SaveFile left, SaveFile right, string label)
    {
        var result = new SaveDiffUseCase().Execute(left, right);
        if (!result.Success)
        {
            Changes = [];
            StatusText = result.Error ?? "These saves cannot be compared.";
            return;
        }

        Changes = new ObservableCollection<SaveDiffChangeRow>(result.Changes.Select(c => new SaveDiffChangeRow(c)));
        StatusText = Changes.Count == 0
            ? $"No differences found. ({label})"
            : $"{Changes.Count} difference(s) found. ({label})";
    }
}
