using PKHeX.Application.Abstractions;
using PKHeX.Core;

namespace PKHeX.Infrastructure;

public sealed class SaveFileService : ISaveFileGateway
{
    public SaveFile? CurrentSave { get; private set; }
    public bool HasSave => CurrentSave is not null;

    private string? _currentPath;

    public event Action<SaveFile?>? SaveFileChanged;

    public async Task<bool> LoadSaveFileAsync(string path)
    {
        var sav = await Task.Run(() =>
        {
            try
            {
                var obj = FileUtil.GetSupportedFile(path);
                return obj as SaveFile;
            }
            catch
            {
                return null;
            }
        });

        if (sav is null)
            return false;

        CurrentSave = sav;
        _currentPath = path;
        SaveFileChanged?.Invoke(CurrentSave);

        return true;
    }

    public void OpenLoadedSave(SaveFile sav, string? path = null)
    {
        CurrentSave = sav;
        _currentPath = path ?? sav.Metadata.FilePath;
        SaveFileChanged?.Invoke(CurrentSave);
    }

    public Task<bool> SaveFileAsync(string? path = null)
    {
        return Task.Run(() =>
        {
            if (CurrentSave is null)
                return false;

            try
            {
                var savePath = path ?? _currentPath;
                if (string.IsNullOrEmpty(savePath))
                    return false;

                var data = CurrentSave.Write().ToArray();
                File.WriteAllBytes(savePath, data);

                if (path is not null)
                    _currentPath = path;

                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public void CloseSave()
    {
        CurrentSave = null;
        _currentPath = null;
        SaveFileChanged?.Invoke(null);
    }
}
