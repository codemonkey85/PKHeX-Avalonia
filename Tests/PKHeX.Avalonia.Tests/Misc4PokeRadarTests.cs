using PKHeX.Presentation.ViewModels;
using PKHeX.Core;
using Xunit;

namespace PKHeX.Avalonia.Tests;

public class Misc4PokeRadarTests
{
    private const ushort PokeRadarItem = 431;

    private static InventoryPouch4 KeyItemsPouch(SaveFile sav) =>
        (InventoryPouch4)sav.Inventory.Pouches.First(p => p.Type is InventoryType.KeyItems);

    [Fact]
    public void Misc4_PokeRadar_Pt_ToggleOn_AddsItemToKeyItems()
    {
        // Arrange
        var sav = new SAV4Pt();
        var vm = new Misc4EditorViewModel(sav);

        // Act
        vm.PokeRadar = true;
        vm.SaveCommand.Execute(null);

        // Assert
        var pouch = KeyItemsPouch(sav);
        Assert.True(pouch.HasItem(PokeRadarItem));
        Assert.Equal(1, pouch.Items.First(it => it.Index == PokeRadarItem).Count);
    }

    [Fact]
    public void Misc4_PokeRadar_Pt_ToggleOff_RemovesItemFromKeyItems()
    {
        // Arrange
        var sav = new SAV4Pt();
        var vm = new Misc4EditorViewModel(sav);
        vm.PokeRadar = true;
        vm.SaveCommand.Execute(null);

        // Act
        vm.PokeRadar = false;
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.False(KeyItemsPouch(sav).HasItem(PokeRadarItem));
    }

    [Fact]
    public void Misc4_PokeRadar_Pt_LoadsExistingState()
    {
        // Arrange
        var sav = new SAV4Pt();
        var bag = sav.Inventory;
        var pouch = (InventoryPouch4)bag.Pouches.First(p => p.Type is InventoryType.KeyItems);
        pouch.Items[0].Index = PokeRadarItem;
        pouch.Items[0].Count = 1;
        bag.CopyTo(sav);

        // Act
        var vm = new Misc4EditorViewModel(sav);

        // Assert
        Assert.True(vm.PokeRadar);
    }

    [Fact]
    public void Misc4_PokeRadar_DP_ToggleOn_AddsItemToKeyItems()
    {
        // Arrange
        var sav = new SAV4DP();
        var vm = new Misc4EditorViewModel(sav);

        // Act
        vm.PokeRadar = true;
        vm.SaveCommand.Execute(null);

        // Assert
        var pouch = KeyItemsPouch(sav);
        Assert.True(pouch.HasItem(PokeRadarItem));
        Assert.Equal(1, pouch.Items.First(it => it.Index == PokeRadarItem).Count);
    }

    [Fact]
    public void Misc4_PokeRadar_DP_ToggleOff_RemovesItemFromKeyItems()
    {
        // Arrange
        var sav = new SAV4DP();
        var vm = new Misc4EditorViewModel(sav);
        vm.PokeRadar = true;
        vm.SaveCommand.Execute(null);

        // Act
        vm.PokeRadar = false;
        vm.SaveCommand.Execute(null);

        // Assert
        Assert.False(KeyItemsPouch(sav).HasItem(PokeRadarItem));
    }

    [Fact]
    public void Misc4_PokeRadar_NotVisible_ForHGSS()
    {
        // Arrange
        var sav = new SAV4HGSS();

        // Act
        var vm = new Misc4EditorViewModel(sav);

        // Assert
        Assert.False(vm.IsPokeRadarVisible);
        Assert.False(vm.PokeRadar);
    }
}
