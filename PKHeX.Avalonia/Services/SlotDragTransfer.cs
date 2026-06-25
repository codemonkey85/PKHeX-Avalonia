using Avalonia.Input;
using PKHeX.Application.Abstractions;
using PKHeX.Presentation.Models;

namespace PKHeX.Avalonia.Services;

/// <summary>
/// Builds and reads the <see cref="DataTransfer"/> payload used for Pokémon slot
/// drag-and-drop. Avalonia 11.3's data-transfer model only supports byte/string
/// application formats (it no longer carries arbitrary CLR objects), so the
/// <see cref="SlotLocation"/> is serialized to a compact string.
/// </summary>
internal static class SlotDragTransfer
{
    private static readonly DataFormat<string> Format =
        DataFormat.CreateStringApplicationFormat("PKHeX.SlotDragData");

    /// <summary>Creates the drag payload for the given slot data.</summary>
    public static DataTransfer Create(SlotDragData data)
    {
        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(Format, Serialize(data.Source)));
        return transfer;
    }

    /// <summary>Reads the slot data back from a drop, or null if it isn't present/valid.</summary>
    public static SlotDragData? TryGet(IDataTransfer? transfer)
    {
        if (transfer?.TryGetValue(Format) is { } raw && TryDeserialize(raw, out var source))
            return new SlotDragData(source);
        return null;
    }

    private static string Serialize(SlotLocation loc)
        => $"{(loc.IsParty ? 1 : 0)}:{loc.Box}:{loc.Slot}";

    private static bool TryDeserialize(string raw, out SlotLocation source)
    {
        source = default;
        var parts = raw.Split(':');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var isParty)
            || !int.TryParse(parts[1], out var box)
            || !int.TryParse(parts[2], out var slot))
        {
            return false;
        }

        source = new SlotLocation { Box = box, Slot = slot, IsParty = isParty != 0 };
        return true;
    }
}
