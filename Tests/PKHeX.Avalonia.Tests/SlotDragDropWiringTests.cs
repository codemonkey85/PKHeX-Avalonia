using Xunit.Abstractions;

namespace PKHeX.Avalonia.Tests;

/// <summary>
/// Structural guardrail for the box/party/editor drag-and-drop fix: a pragmatic, regex-free
/// source scan of the .axaml views (mirroring <see cref="AccessibilityAuditTests"/>'s approach)
/// confirming the drop-target wiring actually landed where the fix requires it:
///  - DragDrop.AllowDrop lives on the slot Button (BoxViewer/PartyViewer) so it, not a child
///    visual, is the drop target once its content is made hit-test-transparent.
///  - The slot content (sprite image + overlays) is IsHitTestVisible="False" so it doesn't
///    swallow the hit before it reaches the AllowDrop Button.
///  - PokemonEditor's AllowDrop lives on the inner Border (not the root UserControl).
///  - DragDrop.DragOver handlers are wired for drop-cursor feedback.
/// This is a static-source check, not a rendered/headless behavioral test — full drag/drop
/// verification happens by hand in a published .app (see PR description).
/// </summary>
public class SlotDragDropWiringTests(ITestOutputHelper output)
{
    [Fact]
    public void BoxViewer_SlotButton_HasAllowDropAndDragOverAndTransparentContent()
    {
        var xml = ReadView("BoxViewer.axaml");

        Assert.Contains("DragDrop.AllowDrop=\"True\"", xml);
        Assert.Contains("DragDrop.Drop=\"OnSlotDrop\"", xml);
        Assert.Contains("DragDrop.DragOver=\"OnSlotDragOver\"", xml);

        // The sprite/overlay content Panel inside the slot Button must not intercept hit-testing.
        var contentPanelIndex = xml.IndexOf("<Panel HorizontalAlignment=\"Stretch\" VerticalAlignment=\"Stretch\" IsHitTestVisible=\"False\">", StringComparison.Ordinal);
        Assert.True(contentPanelIndex >= 0, "Expected the slot content Panel to be IsHitTestVisible=\"False\".");
        output.WriteLine("BoxViewer.axaml: slot Button carries AllowDrop/Drop/DragOver, content Panel is hit-test-transparent ✓");
    }

    [Fact]
    public void PartyViewer_SlotButton_HasAllowDropAndDragOverAndTransparentContent()
    {
        var xml = ReadView("PartyViewer.axaml");

        Assert.Contains("DragDrop.AllowDrop=\"True\"", xml);
        Assert.Contains("DragDrop.Drop=\"OnSlotDrop\"", xml);
        Assert.Contains("DragDrop.DragOver=\"OnSlotDragOver\"", xml);

        // The local Button template's Border root must be hit-test-transparent (Fluent's default
        // Button template has its own opaque Border that would otherwise swallow the hit before
        // it reaches the Button element that actually carries AllowDrop).
        Assert.Contains("<Border Name=\"PART_Border\"", xml);
        Assert.Contains("IsHitTestVisible=\"False\"", xml);
        output.WriteLine("PartyViewer.axaml: slot Button carries AllowDrop/Drop/DragOver, local template root is hit-test-transparent ✓");
    }

    [Fact]
    public void BoxViewer_SlotTemplate_InThemeAxaml_HasTransparentRoot()
    {
        var repoRoot = FindRepoRoot();
        var themePath = Path.Combine(repoRoot, "PKHeX.Avalonia", "Styles", "Theme.axaml");
        var xml = File.ReadAllText(themePath);

        var styleIndex = xml.IndexOf("Selector=\"Button.slot\"", StringComparison.Ordinal);
        Assert.True(styleIndex >= 0, "Expected a Button.slot style in Theme.axaml.");

        var templateSlice = xml[styleIndex..Math.Min(xml.Length, styleIndex + 1500)];
        Assert.Contains("<Panel IsHitTestVisible=\"False\">", templateSlice);
        output.WriteLine("Theme.axaml: Button.slot template root Panel is hit-test-transparent ✓");
    }

    [Fact]
    public void PokemonEditor_AllowDrop_IsOnInnerBorder_NotRootUserControl()
    {
        var xml = ReadView("PokemonEditor.axaml");

        var userControlOpenTag = xml[..xml.IndexOf(">", StringComparison.Ordinal)];
        Assert.DoesNotContain("DragDrop.AllowDrop", userControlOpenTag);

        var borderIndex = xml.IndexOf("<Border Padding=\"6\" Classes=\"view-container\"", StringComparison.Ordinal);
        Assert.True(borderIndex >= 0, "Expected the editor's outer Border.");
        var borderSlice = xml[borderIndex..Math.Min(xml.Length, borderIndex + 400)];
        Assert.Contains("DragDrop.AllowDrop=\"True\"", borderSlice);
        Assert.Contains("DragDrop.Drop=\"OnEditorDrop\"", borderSlice);
        Assert.Contains("DragDrop.DragOver=\"OnEditorDragOver\"", borderSlice);
        output.WriteLine("PokemonEditor.axaml: AllowDrop lives on the inner Border, not the root UserControl ✓");
    }

    private static string ReadView(string fileName)
    {
        var repoRoot = FindRepoRoot();
        var path = Path.Combine(repoRoot, "PKHeX.Avalonia", "Views", fileName);
        Assert.True(File.Exists(path), $"View not found: {path}");
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!Directory.Exists(Path.Combine(dir, "PKHeX.Avalonia")))
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) throw new DirectoryNotFoundException("Could not find repository root");
            dir = parent.FullName;
        }
        return dir;
    }
}
