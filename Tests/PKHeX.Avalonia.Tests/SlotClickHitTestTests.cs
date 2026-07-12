using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Moq;
using PKHeX.Avalonia.Services;
using PKHeX.Avalonia.Views;
using PKHeX.Core;
using PKHeX.Presentation.Models;
using PKHeX.Presentation.ViewModels;
using Xunit.Abstractions;

namespace PKHeX.Avalonia.Tests;

/// <summary>
/// Behavioral (headless, real layout + real hit-testing) regression guard for the box/party slot
/// click that PR #169 broke. The slot Button's ControlTemplate (Theme.axaml Button.slot for boxes,
/// the local template in PartyViewer.axaml for party) must have a hit-test-visible root carrying a
/// non-null Background so a pointer press/release inside the slot resolves to the Button and its
/// Click fires. PR #169 set the template root <c>IsHitTestVisible="False"</c>, which excluded the
/// Button's entire visual subtree from hit-testing, killing every slot click (and, since drop
/// resolution walks up from the hit element, drops too).
///
/// These tests simulate a real mouse press/release at the slot's on-screen center via
/// Avalonia.Headless input and assert the ViewModel selection actually moves — which only happens
/// if the click routed through hit-testing to the Button. With the defect present the click lands
/// on the non-hittable subtree, the Button never fires, and the assertion fails.
/// </summary>
public class SlotClickHitTestTests(ITestOutputHelper output)
{
    private static Mock<ISpriteRenderer> SpriteMock() => new();

    [AvaloniaFact]
    public void BoxViewer_ClickingFilledSlot_RoutesToButton_AndSelectsSlot()
    {
        const int targetSlot = 3; // not the default-selected slot 0, so a real change is observable

        var sav = new SAV6XY();
        sav.SetBoxSlotAtIndex(new PK6 { Species = 25 }, targetSlot); // Pikachu in box 0, slot 3
        var vm = new BoxViewerViewModel(sav, SpriteMock().Object);
        var view = new BoxViewer { DataContext = vm };

        var window = new Window { Content = view, Width = 720, Height = 640 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, vm.SelectedIndex); // baseline

        var button = view.GetVisualDescendants()
            .OfType<Button>()
            .First(b => b.Tag is SlotData sd && sd.Slot == targetSlot);

        ClickCenter(window, button);

        Assert.True(button.Bounds.Width > 0 && button.Bounds.Height > 0,
            "Slot button was not laid out; the test setup, not the fix, is at fault.");
        Assert.Equal(targetSlot, vm.SelectedIndex);
        Assert.True(vm.Slots[targetSlot].IsSelected, "Clicked slot should be selected.");
        output.WriteLine($"BoxViewer: click at slot {targetSlot} center routed to Button, SelectedIndex={vm.SelectedIndex} ✓");
    }

    [AvaloniaFact]
    public void PartyViewer_ClickingSlot_RoutesToButton_AndSelectsSlot()
    {
        const int targetSlot = 2;

        var sav = new SAV6XY();
        var vm = new PartyViewerViewModel(sav, SpriteMock().Object);
        var view = new PartyViewer { DataContext = vm };

        var window = new Window { Content = view, Width = 520, Height = 640 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, vm.SelectedIndex);

        var button = view.GetVisualDescendants()
            .OfType<Button>()
            .First(b => b.Tag is PartySlotData sd && sd.Slot == targetSlot);

        ClickCenter(window, button);

        Assert.True(button.Bounds.Width > 0 && button.Bounds.Height > 0,
            "Slot button was not laid out; the test setup, not the fix, is at fault.");
        Assert.Equal(targetSlot, vm.SelectedIndex);
        Assert.True(vm.Slots[targetSlot].IsSelected, "Clicked slot should be selected.");
        output.WriteLine($"PartyViewer: click at slot {targetSlot} center routed to Button, SelectedIndex={vm.SelectedIndex} ✓");
    }

    private static void ClickCenter(Window window, Control target)
    {
        var center = target.TranslatePoint(
            new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window);
        Assert.NotNull(center);
        window.MouseDown(center.Value, MouseButton.Left);
        window.MouseUp(center.Value, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
