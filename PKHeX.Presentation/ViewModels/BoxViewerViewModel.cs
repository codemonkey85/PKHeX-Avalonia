using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Presentation.Models;
using PKHeX.Core;
using System.Collections.ObjectModel;

namespace PKHeX.Presentation.ViewModels;

public partial class BoxViewerViewModel : ViewModelBase
{
    private readonly SaveFile _sav;
    private readonly ISpriteRenderer _spriteRenderer;
    private readonly ISlotService? _slotService;

    private const int Columns = 6;

    [ObservableProperty]
    private int _currentBox;

    [ObservableProperty]
    private string _boxName = string.Empty;

    [ObservableProperty]
    private int _selectedIndex;

    [ObservableProperty]
    private ObservableCollection<SlotData> _slots = [];

    /// <summary>Whether the in-save seek bar is expanded.</summary>
    [ObservableProperty]
    private bool _isSeekBarVisible;

    /// <summary>Feedback text for the most recent seek (e.g. "Found in Box 2" / "No matches").</summary>
    [ObservableProperty]
    private string _seekStatus = string.Empty;

    /// <summary>Shared filter inputs that build the in-save seek predicate (bound by the view).</summary>
    public EntityFilterViewModel Filter { get; }

    public int BoxCount => _sav.BoxCount;
    public int SlotsPerBox => _sav.BoxSlotCount;

    public BoxViewerViewModel(SaveFile sav, ISpriteRenderer spriteRenderer, ISlotService? slotService = null)
    {
        _sav = sav;
        _spriteRenderer = spriteRenderer;
        _slotService = slotService;
        Filter = new EntityFilterViewModel(sav);

        LoadBox(0);
    }

    partial void OnSelectedIndexChanged(int value)
    {
        for (int i = 0; i < Slots.Count; i++)
            Slots[i].IsSelected = i == value;
    }

    private void LoadBox(int box)
    {
        if (box < 0 || box >= BoxCount)
            return;

        var previousIndex = SelectedIndex;
        CurrentBox = box;
        BoxName = _sav is IBoxDetailNameRead r
            ? r.GetBoxName(box)
            : BoxDetailNameExtensions.GetDefaultBoxName(box);

        Slots.Clear();

        var boxData = _sav.GetBoxData(box);

        for (int slot = 0; slot < boxData.Length; slot++)
        {
            var pk = boxData[slot];
            var isEmpty = pk.Species == 0;

            // Use StringResourceLookup for all string-table accesses to safely
            // handle Gen 1/2 where Ability is -1 and some properties are placeholders.
            var slotData = new SlotData
            {
                Slot = slot,
                Box = box,
                Species = pk.Species,
                Sprite = _spriteRenderer.GetSprite(pk),
                IsEmpty = isEmpty,
                IsShiny = pk.IsShiny,
                Nickname = isEmpty ? string.Empty : pk.Nickname,
                SpeciesName = isEmpty ? string.Empty : StringResourceLookup.Species(pk.Species),
                Level = pk.CurrentLevel,
                Gender = (byte)pk.Gender,
                HeldItem = (ushort)pk.HeldItem,
                HeldItemName = pk.HeldItem > 0 ? StringResourceLookup.Item(pk.HeldItem) : string.Empty,
                IsEgg = pk.IsEgg,
                Form = pk.Form,
                Ability = (ushort)pk.Ability,
                AbilityName = StringResourceLookup.Ability(pk.Ability),
                Nature = (byte)pk.Nature,
                NatureName = StringResourceLookup.Nature((int)pk.Nature),
                ShowdownSummary = isEmpty ? string.Empty : new ShowdownSet(pk).Text,
                IsLegal = isEmpty || new LegalityAnalysis(pk).Valid,
                IsSelected = false,
            };

            Slots.Add(slotData);
        }

        // Restore selection position (clamped to valid range)
        SelectedIndex = Math.Clamp(previousIndex, 0, Math.Max(0, Slots.Count - 1));
    }

    [RelayCommand]
    private void PreviousBox()
    {
        var newBox = CurrentBox - 1;
        if (newBox < 0)
            newBox = BoxCount - 1;
        LoadBox(newBox);
    }

    [RelayCommand]
    private void NextBox()
    {
        var newBox = CurrentBox + 1;
        if (newBox >= BoxCount)
            newBox = 0;
        LoadBox(newBox);
    }

    [RelayCommand]
    private void ToggleSeekBar()
    {
        IsSeekBarVisible = !IsSeekBarVisible;
        if (!IsSeekBarVisible)
            SeekStatus = string.Empty;
    }

    [RelayCommand]
    private void SeekNext() => Seek(reverse: false);

    [RelayCommand]
    private void SeekPrevious() => Seek(reverse: true);

    /// <summary>
    /// Jumps to and highlights the next/previous box slot matching the current filter,
    /// wrapping across boxes (party slots are excluded — they live in a separate viewer).
    /// Mirrors upstream's EntitySearchControl in-place seek behaviour.
    /// </summary>
    private void Seek(bool reverse)
    {
        var predicate = Filter.CreateSearchPredicate();
        int boxCount = _sav.BoxCount;
        int perBox = _sav.BoxSlotCount;
        int totalBoxSlots = boxCount * perBox;
        if (totalBoxSlots == 0)
        {
            SeekStatus = "No box slots.";
            return;
        }

        int step = reverse ? -1 : 1;
        int current = (CurrentBox * perBox) + SelectedIndex;

        for (int i = 1; i <= totalBoxSlots; i++)
        {
            int index = ((current + (i * step)) % totalBoxSlots + totalBoxSlots) % totalBoxSlots;
            int box = index / perBox;
            int slot = index % perBox;

            var pk = _sav.GetBoxSlotAtIndex(box, slot);
            if (pk.Species == 0)
                continue;
            if (!predicate(pk))
                continue;

            if (box != CurrentBox)
                LoadBox(box);
            SelectedIndex = slot;
            SeekStatus = $"Found in {BoxName} (slot {slot + 1}).";
            return;
        }

        SeekStatus = "No matches.";
    }

    [RelayCommand]
    private void SelectSlotByClick(SlotData? slot)
    {
        if (slot is null)
            return;

        SelectedIndex = slot.Slot;
    }

    [RelayCommand]
    private void MoveSelection(string direction)
    {
        if (Slots.Count == 0) return;

        int newIndex = direction switch
        {
            "Left" => SelectedIndex > 0 ? SelectedIndex - 1 : SelectedIndex,
            "Right" => SelectedIndex < Slots.Count - 1 ? SelectedIndex + 1 : SelectedIndex,
            "Up" => SelectedIndex >= Columns ? SelectedIndex - Columns : SelectedIndex,
            "Down" => SelectedIndex + Columns < Slots.Count ? SelectedIndex + Columns : SelectedIndex,
            _ => SelectedIndex
        };

        SelectedIndex = newIndex;
    }

    [RelayCommand]
    private void SelectFirstSlot()
    {
        if (Slots.Count > 0)
            SelectedIndex = 0;
    }

    [RelayCommand]
    private void SelectLastSlot()
    {
        if (Slots.Count > 0)
            SelectedIndex = Slots.Count - 1;
    }

    [RelayCommand]
    private void ActivateSlot()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Slots.Count)
            return;

        var slot = Slots[SelectedIndex];
        SlotActivated?.Invoke(CurrentBox, slot.Slot);
    }

    public void RefreshCurrentBox()
    {
        LoadBox(CurrentBox);
    }

    public event Action<int, int>? SlotActivated;
    public event Action<int, int>? ViewSlotRequested;
    public event Action<int, int>? SetSlotRequested;
    public event Action<int, int>? DeleteSlotRequested;
    
    [RelayCommand]
    private void ViewSlot(SlotData? slot)
    {
        if (slot is null || slot.IsEmpty)
            return;
        
        if (_slotService is not null)
            _slotService.RequestView(SlotLocation.FromBox(CurrentBox, slot.Slot));
        else
            ViewSlotRequested?.Invoke(CurrentBox, slot.Slot);
    }
    
    [RelayCommand]
    private void SetSlot(SlotData? slot)
    {
        if (slot is null)
            return;
        
        if (_slotService is not null)
            _slotService.RequestSet(SlotLocation.FromBox(CurrentBox, slot.Slot));
        else
            SetSlotRequested?.Invoke(CurrentBox, slot.Slot);
    }
    
    [RelayCommand]
    private void DeleteSlot(SlotData? slot)
    {
        if (slot is null || slot.IsEmpty)
            return;
        
        if (_slotService is not null)
            _slotService.RequestDelete(SlotLocation.FromBox(CurrentBox, slot.Slot));
        else
            DeleteSlotRequested?.Invoke(CurrentBox, slot.Slot);
    }
    
    public PKM GetSlotPKM(int slot) => _sav.GetBoxSlotAtIndex(CurrentBox, slot);

    public void SetSlotPKM(int slot, PKM pk)
    {
        _sav.SetBoxSlotAtIndex(pk, CurrentBox, slot);
        RefreshCurrentBox();
    }

    public void ClearSlot(int slot)
    {
        _sav.SetBoxSlotAtIndex(_sav.BlankPKM, CurrentBox, slot);
        RefreshCurrentBox();
    }

    [RelayCommand]
    private void RequestMove((SlotDragData data, SlotData dest, bool clone) param)
    {
        _slotService?.RequestMove(param.data.Source, param.dest.Location, param.clone);
    }
}
