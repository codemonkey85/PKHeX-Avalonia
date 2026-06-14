using PKHeX.Core;

namespace PKHeX.Application.Services;

/// <summary>
/// Application service wrapping Core's <see cref="SlotChangelog"/>. Exposes undo/redo state via a
/// plain <see cref="StateChanged"/> event (no MVVM-framework dependency).
/// </summary>
public sealed class UndoRedoService
{
    private SlotChangelog? _changelog;
    private int _changeCount;

    public int ChangeCount => _changeCount;
    public bool CanUndo => _changelog?.CanUndo ?? false;
    public bool CanRedo => _changelog?.CanRedo ?? false;

    /// <summary>Raised whenever undo/redo availability may have changed.</summary>
    public event EventHandler? StateChanged;
    public event Action<ISlotInfo>? UndoPerformed;
    public event Action<ISlotInfo>? RedoPerformed;

    private void SetChangeCount(int value)
    {
        _changeCount = value;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Initialize(SaveFile sav)
    {
        _changelog = new SlotChangelog(sav);
        SetChangeCount(0);
    }

    public void Clear()
    {
        _changelog = null;
        SetChangeCount(0);
    }

    public void AddChange(ISlotInfo info)
    {
        _changelog?.AddNewChange(info);
        SetChangeCount(_changeCount + 1);
    }

    public void Undo()
    {
        if (_changelog is null || !_changelog.CanUndo) return;

        var info = _changelog.Undo();
        SetChangeCount(_changeCount + 1);
        UndoPerformed?.Invoke(info);
    }

    public void Redo()
    {
        if (_changelog is null || !_changelog.CanRedo) return;

        var info = _changelog.Redo();
        SetChangeCount(_changeCount + 1);
        RedoPerformed?.Invoke(info);
    }
}
