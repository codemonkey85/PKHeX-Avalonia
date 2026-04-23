using Moq;
using PKHeX.Avalonia.Services;
using PKHeX.Avalonia.Tests.Fixtures;
using PKHeX.Avalonia.ViewModels;
using PKHeX.Core;
using Xunit.Abstractions;

namespace PKHeX.Avalonia.Tests;

/// <summary>
/// Regression tests for issue #69 — Gen 1/Gen 2 boxes rendered empty with red
/// warning triangles because <c>GameInfo.Strings.Ability[-1]</c> threw and the
/// exception was swallowed by a broad try/catch, silently marking every slot
/// empty.
///
/// These tests pin the behavior: for any Gen 1/2 save with occupied slots, the
/// BoxViewer must populate <c>SlotData.Species</c> and must not mark all slots
/// <c>IsEmpty</c>.
/// </summary>
public class Gen1Gen2BoxRenderingTests(ITestOutputHelper output)
{
    private static readonly string[] Gen12SaveFiles =
    [
        "gen1_red.sav",
        "gen2_gold.sav",
        "gen2_crystal.sav",
    ];

    public static IEnumerable<object[]> Gen12Saves()
    {
        var dir = SaveFileFixture.FindSaveFilesPath();
        if (dir == null) yield break;

        foreach (var name in Gen12SaveFiles)
        {
            var path = Path.Combine(dir, name);
            var sav = SaveFileFixture.LoadSave(path);
            if (sav == null) continue;
            yield return [sav, name];
        }
    }

    /// <summary>
    /// Direct Gen 1/2 regression check against the exact lookup paths that used
    /// to crash. Runs even when no save files are present.
    /// </summary>
    [Fact]
    public void StringResourceLookup_HandlesNegativeAndOverflow()
    {
        // Gen 1/2 PKM.Ability returns -1
        Assert.Equal(string.Empty, StringResourceLookup.Ability(-1));
        Assert.Equal(string.Empty, StringResourceLookup.Ability(int.MinValue));
        Assert.Equal(string.Empty, StringResourceLookup.Ability(999999));

        // Species/Item/Move/Nature should also tolerate out-of-range
        Assert.Equal(string.Empty, StringResourceLookup.Item(-1));
        Assert.Equal(string.Empty, StringResourceLookup.Move(-1));
        Assert.Equal(string.Empty, StringResourceLookup.Nature(-1));
        Assert.Equal(string.Empty, StringResourceLookup.Species(ushort.MaxValue));

        // Valid indices still return real names
        Assert.False(string.IsNullOrEmpty(StringResourceLookup.Species(25))); // Pikachu
        Assert.False(string.IsNullOrEmpty(StringResourceLookup.Ability(1)));
    }

    /// <summary>
    /// For a real Gen 1/2 save with occupied slots, BoxViewer must surface those
    /// slots — not mark them empty. This was the user-visible symptom of #69.
    /// Also walks every box via NextBoxCommand so we cover all boxes, not just the first.
    /// </summary>
    [Theory, MemberData(nameof(Gen12Saves))]
    public void BoxViewer_Gen12_OccupiedSlotsAreVisible(SaveFile sav, string label)
    {
        var totalOccupied = SaveFileFixture.CountOccupiedSlots(sav);
        output.WriteLine($"{label}: {totalOccupied} occupied slots in save");
        if (totalOccupied == 0)
        {
            output.WriteLine("  empty save — nothing to verify");
            return;
        }

        var sprite = new Mock<ISpriteRenderer>();
        var vm = new BoxViewerViewModel(sav, sprite.Object);

        int visibleAcrossBoxes = 0;
        for (int b = 0; b < sav.BoxCount; b++)
        {
            // vm starts at box 0 (from constructor); walk forward via command
            foreach (var slot in vm.Slots)
            {
                var raw = sav.GetBoxSlotAtIndex(vm.CurrentBox, slot.Slot);
                var rawOccupied = raw.Species != 0;

                Assert.Equal(rawOccupied, slot.IsEmpty == false);
                if (!slot.IsEmpty)
                {
                    Assert.Equal(raw.Species, slot.Species);
                    visibleAcrossBoxes++;
                }
            }

            // Advance to next box for the next iteration (wraps; that's fine)
            if (vm.NextBoxCommand.CanExecute(null))
                vm.NextBoxCommand.Execute(null);
        }

        Assert.Equal(totalOccupied, visibleAcrossBoxes);
        output.WriteLine($"  {visibleAcrossBoxes} slots visible in BoxViewer (matches save)");
    }

    /// <summary>
    /// Clicking an occupied Gen 1/2 slot must load a non-empty PKM into the
    /// editor. With the old bug, the slot was marked empty so the click path
    /// short-circuited on <c>Species == 0</c>.
    /// </summary>
    [Theory, MemberData(nameof(Gen12Saves))]
    public void PokemonEditor_Gen12_LoadsFromOccupiedSlot(SaveFile sav, string label)
    {
        var first = SaveFileFixture.GetFirstOccupiedSlot(sav);
        if (first == null)
        {
            output.WriteLine($"{label}: no occupied slots, skipping");
            return;
        }

        var (pk, idx) = first.Value;
        output.WriteLine($"{label}: slot {idx} species={pk.Species} ability={pk.Ability}");

        var (vm, _, _) = TestHelpers.CreateTestViewModel(pk, sav);
        Assert.NotEqual(0, vm.Species);
        Assert.NotEqual("Empty Slot", vm.Title);
        _ = vm.IsLegal; // must not throw
    }
}
