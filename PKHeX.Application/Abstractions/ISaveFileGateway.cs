using PKHeX.Core;

namespace PKHeX.Application.Abstractions;

public interface ISaveFileGateway
{
    SaveFile? CurrentSave { get; }
    bool HasSave { get; }

    Task<bool> LoadSaveFileAsync(string path);

    /// <summary>
    /// Adopts an already-constructed <see cref="SaveFile"/> as the current save, bypassing path-based
    /// auto-detection. Used by the Save Handler Troubleshooter to open a save that the normal loader
    /// could not recognize.
    /// </summary>
    /// <param name="sav">The save file to make current.</param>
    /// <param name="path">Optional originating path, recorded so a later Save writes back to the right file.</param>
    void OpenLoadedSave(SaveFile sav, string? path = null);

    Task<bool> SaveFileAsync(string? path = null);
    void CloseSave();

    event Action<SaveFile?>? SaveFileChanged;
}
