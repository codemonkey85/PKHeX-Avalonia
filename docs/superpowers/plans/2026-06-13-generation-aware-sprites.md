# Generation-aware Pokémon Sprites Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make PKHeX-Avalonia render Pokémon using the sprite style that matches the loaded save (HOME artwork for Scarlet/Violet & Legends Z-A, circular mugshots for Legends: Arceus, classic box sprites for everything else), with a user setting to override — faithfully porting upstream PKHeX's behavior.

**Architecture:** A 3-value `SpriteStyle` and a static `SpriteStyleSelector` (Avalonia layer, mirroring upstream `SpriteBuilderUtil.GetSuggestedMode`) decide the style from the save type. The existing `SpriteLoader` gains a `Style` property and a per-style folder/letter table instead of hardcoded classic paths; it is set once per save load in `AvaloniaSpriteRenderer.Initialize`. A `SpritePreference` setting in `AppSettings` (surfaced via a Settings ComboBox) overrides the auto-pick and applies live. All three sprite sets are already bundled in the repo; no assets are added.

**Tech Stack:** .NET 10, C#, Avalonia 11.2, SkiaSharp (sprite decoding), CommunityToolkit.Mvvm (`[ObservableProperty]`), xUnit + Avalonia.Headless.XUnit (tests). Spritework lives entirely in `PKHeX.Avalonia` — `PKHeX.Core` stays 1:1 with upstream (do **not** modify it).

**Spec:** `docs/superpowers/specs/2026-06-13-generation-aware-sprites-design.md`

---

## Background facts the engineer needs

- Sprite PNGs are **embedded resources** under `PKHeX.Avalonia/Assets/Images/`. The manifest resource name replaces spaces with underscores, e.g. the file `Assets/Images/Big Pokemon Sprites/b_25.png` → resource `PKHeX.Avalonia.Assets.Images.Big_Pokemon_Sprites.b_25.png`.
- Three Pokémon sprite sets exist, all **68×56**:
  | Style | normal folder | shiny folder | filename letter | egg file |
  |---|---|---|---|---|
  | Classic | `Big_Pokemon_Sprites.` | `Big_Shiny_Sprites.` | `b` | `b_egg.png` |
  | Mugshot | `Legends_Arceus_Sprites.` | `Legends_Arceus_Shiny_Sprites.` | `c` | `c_egg.png` |
  | Artwork | `Artwork_Pokemon_Sprites.` | *(none — no shiny set bundled)* | `a` | `a_egg.png` |
- **Shiny filenames carry an `s` suffix** (`b_25s.png`, `b_25-1cs.png`, `c_25s.png`). The current loader looks up `b_25.png` inside the shiny folder, so shiny sprites never load today — this plan fixes that.
- Coverage: Classic & Mugshot cover species ≤ #905; Artwork covers the full dex to #1025. Forcing Classic/Mugshot on a Gen 9 save shows the placeholder for #906–1025 (expected, matches upstream).
- `SaveFile` save-type classes used for the switch: `SAV8LA`, `SAV9SV`, `SAV9ZA`, `SAV8SWSH` (all have parameterless constructors). `EntityContext` lives in `PKHeX.Core`.
- DI: `AppSettings` is a singleton (`App.axaml.cs:44-46`), and `ISpriteRenderer → AvaloniaSpriteRenderer` is a singleton (`App.axaml.cs:51`). Microsoft DI resolves constructor parameters, so adding an `AppSettings` parameter to `AvaloniaSpriteRenderer` needs no registration change. Nothing constructs `AvaloniaSpriteRenderer` or `SpriteLoader` directly (verified), so constructor changes are safe.
- Test project: `Tests/PKHeX.Avalonia.Tests/PKHeX.Avalonia.Tests.csproj`, xUnit, global `using Xunit;`, references both `PKHeX.Core` and `PKHeX.Avalonia`. Tests build saves with `new SAV9SV()` etc.

## File structure

| File | Responsibility |
|---|---|
| `PKHeX.Avalonia/Services/SpriteStyle.cs` (new) | `SpriteStyle` + `SpritePreference` enums |
| `PKHeX.Avalonia/Services/SpriteStyleSelector.cs` (new) | suggested/resolve logic (save-type switch) |
| `PKHeX.Avalonia/Services/AppSettings.cs` (modify) | `SpriteSettings` group |
| `PKHeX.Avalonia/Services/SpriteLoader.cs` (modify) | style-aware folders/letter; shiny `s` suffix; style egg |
| `PKHeX.Avalonia/Services/AvaloniaSpriteRenderer.cs` (modify) | inject `AppSettings`; set `_loader.Style` in `Initialize` |
| `PKHeX.Avalonia/Converters/SpritePreferenceConverter.cs` (new) | enum → friendly label |
| `PKHeX.Avalonia/ViewModels/SettingsViewModel.cs` (modify) | preference property + Load/Save |
| `PKHeX.Avalonia/Views/SettingsView.axaml` (modify) | "Sprites" card with ComboBox |
| `PKHeX.Avalonia/ViewModels/PokemonEditorViewModel.cs` (modify) | public `RefreshSprite()` |
| `PKHeX.Avalonia/ViewModels/MainWindowViewModel.EditorDialogs.cs` (modify) | live-apply after Settings closes |
| `Tests/PKHeX.Avalonia.Tests/SpriteStyleTests.cs` (new) | selector + loader behavior tests |
| `Directory.Build.props` (modify) | bump `UIVersion` |

---

### Task 1: Sprite style enums + selector

**Files:**
- Create: `PKHeX.Avalonia/Services/SpriteStyle.cs`
- Create: `PKHeX.Avalonia/Services/SpriteStyleSelector.cs`
- Test: `Tests/PKHeX.Avalonia.Tests/SpriteStyleTests.cs`

- [ ] **Step 1: Write the enums**

Create `PKHeX.Avalonia/Services/SpriteStyle.cs`:

```csharp
namespace PKHeX.Avalonia.Services;

/// <summary>
/// Resolved sprite rendering style. Selects which bundled sprite set <see cref="SpriteLoader"/> reads.
/// </summary>
public enum SpriteStyle
{
    /// <summary>Classic 68×56 box sprites (b_*, "Big Pokemon Sprites").</summary>
    Classic,
    /// <summary>Legends: Arceus circular mugshots (c_*, "Legends Arceus Sprites").</summary>
    Mugshot,
    /// <summary>HOME-style artwork (a_*, "Artwork Pokemon Sprites").</summary>
    Artwork,
}

/// <summary>
/// User preference for sprite style. Mirrors upstream PKHeX's SpriteBuilderPreference (subset).
/// </summary>
public enum SpritePreference
{
    /// <summary>Pick the style automatically from the loaded save file.</summary>
    UseSuggested,
    /// <summary>Always use classic box sprites.</summary>
    ForceSprites,
    /// <summary>Always use Legends: Arceus mugshots.</summary>
    ForceMugshots,
    /// <summary>Always use HOME-style artwork.</summary>
    ForceArtwork,
}
```

- [ ] **Step 2: Write the selector**

Create `PKHeX.Avalonia/Services/SpriteStyleSelector.cs`:

```csharp
using PKHeX.Core;

namespace PKHeX.Avalonia.Services;

/// <summary>
/// Chooses the <see cref="SpriteStyle"/> for a loaded save, mirroring upstream PKHeX's
/// <c>SpriteBuilderUtil.GetSuggestedMode</c> save-type switch.
/// </summary>
public static class SpriteStyleSelector
{
    /// <summary>The style upstream suggests for the given save file.</summary>
    public static SpriteStyle GetSuggested(SaveFile sav) => sav switch
    {
        SAV8LA => SpriteStyle.Mugshot,
        SAV9SV or SAV9ZA => SpriteStyle.Artwork,
        _ => SpriteStyle.Classic,
    };

    /// <summary>
    /// Resolves the effective style from the user preference and the loaded save.
    /// Force* values win; <see cref="SpritePreference.UseSuggested"/> defers to <see cref="GetSuggested"/>.
    /// </summary>
    public static SpriteStyle Resolve(SpritePreference preference, SaveFile sav) => preference switch
    {
        SpritePreference.ForceSprites => SpriteStyle.Classic,
        SpritePreference.ForceMugshots => SpriteStyle.Mugshot,
        SpritePreference.ForceArtwork => SpriteStyle.Artwork,
        _ => GetSuggested(sav),
    };
}
```

- [ ] **Step 3: Write the failing tests**

Create `Tests/PKHeX.Avalonia.Tests/SpriteStyleTests.cs`:

```csharp
using PKHeX.Avalonia.Services;
using PKHeX.Core;

namespace PKHeX.Avalonia.Tests;

public class SpriteStyleTests
{
    [Fact]
    public void GetSuggested_LegendsArceus_ReturnsMugshot()
        => Assert.Equal(SpriteStyle.Mugshot, SpriteStyleSelector.GetSuggested(new SAV8LA()));

    [Fact]
    public void GetSuggested_ScarletViolet_ReturnsArtwork()
        => Assert.Equal(SpriteStyle.Artwork, SpriteStyleSelector.GetSuggested(new SAV9SV()));

    [Fact]
    public void GetSuggested_LegendsZA_ReturnsArtwork()
        => Assert.Equal(SpriteStyle.Artwork, SpriteStyleSelector.GetSuggested(new SAV9ZA()));

    [Fact]
    public void GetSuggested_SwordShield_ReturnsClassic()
        => Assert.Equal(SpriteStyle.Classic, SpriteStyleSelector.GetSuggested(new SAV8SWSH()));

    [Theory]
    [InlineData(SpritePreference.ForceSprites, SpriteStyle.Classic)]
    [InlineData(SpritePreference.ForceMugshots, SpriteStyle.Mugshot)]
    [InlineData(SpritePreference.ForceArtwork, SpriteStyle.Artwork)]
    public void Resolve_ForcePreference_OverridesSave(SpritePreference pref, SpriteStyle expected)
        => Assert.Equal(expected, SpriteStyleSelector.Resolve(pref, new SAV9SV()));

    [Fact]
    public void Resolve_UseSuggested_DefersToSave()
        => Assert.Equal(SpriteStyle.Artwork, SpriteStyleSelector.Resolve(SpritePreference.UseSuggested, new SAV9SV()));
}
```

- [ ] **Step 4: Run the tests**

Run: `dotnet test Tests/PKHeX.Avalonia.Tests/PKHeX.Avalonia.Tests.csproj --filter "FullyQualifiedName~SpriteStyleTests"`
Expected: PASS (6 test cases incl. the 3 `[Theory]` rows). If a save class lacks a parameterless ctor, the build error names it — adjust by constructing it the way sibling tests do.

- [ ] **Step 5: Commit**

```bash
git add PKHeX.Avalonia/Services/SpriteStyle.cs PKHeX.Avalonia/Services/SpriteStyleSelector.cs Tests/PKHeX.Avalonia.Tests/SpriteStyleTests.cs
git commit -m "feat(sprites): add SpriteStyle/SpritePreference enums and save-based selector"
```

---

### Task 2: Add the SpriteSettings group to AppSettings

**Files:**
- Modify: `PKHeX.Avalonia/Services/AppSettings.cs`

This must land before the renderer (Task 4) reads `_settings.Sprite`.

- [ ] **Step 1: Add the observable group field**

In `PKHeX.Avalonia/Services/AppSettings.cs`, the settings-group block currently ends at line 25 (`_localResources`). Add one line after it:

```csharp
    [ObservableProperty] private LocalResourceSettings _localResources = new();
    [ObservableProperty] private SpriteSettings _sprite = new();
```

- [ ] **Step 2: Add the nested SpriteSettings class**

`SpriteSettings` has no upstream Core equivalent (upstream keeps it in WinForms), so define it locally next to the existing local `StartupSettings` nested class. Inside the `AppSettings` class body, after the closing brace of the `StartupSettings` nested class (line ~82) and before the final class-closing brace, add:

```csharp
    /// <summary>
    /// Sprite display preferences. Defined locally since Core does not expose a sprite settings type.
    /// </summary>
    public class SpriteSettings
    {
        public SpritePreference SpritePreference { get; set; } = SpritePreference.UseSuggested;
    }
```

(`SpritePreference` is in the same `PKHeX.Avalonia.Services` namespace — no extra `using` needed.)

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build PKHeX.Avalonia/PKHeX.Avalonia.csproj -clp:ErrorsOnly`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add PKHeX.Avalonia/Services/AppSettings.cs
git commit -m "feat(sprites): add SpriteSettings (sprite preference) to AppSettings"
```

---

### Task 3: Make SpriteLoader style-aware and fix the shiny suffix

**Files:**
- Modify: `PKHeX.Avalonia/Services/SpriteLoader.cs`
- Test: `Tests/PKHeX.Avalonia.Tests/SpriteStyleTests.cs` (extend)

- [ ] **Step 1: Add a `Style` property and per-style resource descriptor**

In `SpriteLoader.cs`, replace the four prefix constants (lines 19-22):

```csharp
    private const string SpritePrefix = "PKHeX.Avalonia.Assets.Images.Big_Pokemon_Sprites.";
    private const string ShinyPrefix = "PKHeX.Avalonia.Assets.Images.Big_Shiny_Sprites.";
    private const string ItemPrefix = "PKHeX.Avalonia.Assets.Images.Big_Items.";
    private const string OverlayPrefix = "PKHeX.Avalonia.Assets.Images.Pokemon_Sprite_Overlays.";
```

with:

```csharp
    private const string ImagePrefix = "PKHeX.Avalonia.Assets.Images.";
    private const string ItemPrefix = ImagePrefix + "Big_Items.";
    private const string OverlayPrefix = ImagePrefix + "Pokemon_Sprite_Overlays.";

    /// <summary>Active sprite style; set per loaded save by <see cref="AvaloniaSpriteRenderer"/>.</summary>
    public SpriteStyle Style { get; set; } = SpriteStyle.Classic;

    private readonly record struct StyleResources(string NormalFolder, string? ShinyFolder, char Letter, string EggFile);

    private static StyleResources GetStyleResources(SpriteStyle style) => style switch
    {
        SpriteStyle.Mugshot => new StyleResources("Legends_Arceus_Sprites.", "Legends_Arceus_Shiny_Sprites.", 'c', "c_egg.png"),
        SpriteStyle.Artwork => new StyleResources("Artwork_Pokemon_Sprites.", null, 'a', "a_egg.png"),
        _ => new StyleResources("Big_Pokemon_Sprites.", "Big_Shiny_Sprites.", 'b', "b_egg.png"),
    };
```

- [ ] **Step 2: Rewrite `GetResourceName` to use the active style + shiny `s` suffix**

Replace `GetResourceName` (lines 123-128):

```csharp
    private string GetResourceName(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context)
    {
        var prefix = shiny ? ShinyPrefix : SpritePrefix;
        var spriteName = GetSpriteName(species, form, gender, formarg, context);
        return $"{prefix}b{spriteName}.png";
    }
```

with:

```csharp
    private string GetResourceName(ushort species, byte form, byte gender, uint formarg, bool shiny, EntityContext context)
    {
        var res = GetStyleResources(Style);
        var spriteName = GetSpriteName(species, form, gender, formarg, context);
        var useShiny = shiny && res.ShinyFolder is not null;
        var folder = useShiny ? res.ShinyFolder! : res.NormalFolder;
        var shinySuffix = useShiny ? "s" : string.Empty;
        return $"{ImagePrefix}{folder}{res.Letter}{spriteName}{shinySuffix}.png";
    }
```

- [ ] **Step 3: Make the species-only fallback style-aware**

In `GetSprite`, replace the ultimate fallback (lines 90-94):

```csharp
        // Ultimate fallback: just species
        if (bitmap is null)
        {
            var speciesOnlyName = $"{SpritePrefix}b_{species}.png";
            bitmap = LoadSprite(speciesOnlyName);
        }
```

with:

```csharp
        // Ultimate fallback: just species, in the active style
        if (bitmap is null)
        {
            var res = GetStyleResources(Style);
            var speciesOnlyName = $"{ImagePrefix}{res.NormalFolder}{res.Letter}_{species}.png";
            bitmap = LoadSprite(speciesOnlyName);
        }
```

- [ ] **Step 4: Make `GetEggSprite` style-aware**

Replace `GetEggSprite` (lines 105-112):

```csharp
    public SKBitmap? GetEggSprite(ushort species)
    {
        // Manaphy has a special egg
        if (species == (ushort)Species.Manaphy)
            return LoadFromPrefix(SpritePrefix, "b_490_e.png");

        return LoadFromPrefix(SpritePrefix, "b_egg.png");
    }
```

with:

```csharp
    public SKBitmap? GetEggSprite(ushort species)
    {
        var res = GetStyleResources(Style);
        var folder = $"{ImagePrefix}{res.NormalFolder}";

        // Manaphy has a special egg sprite, only present in the classic set.
        if (species == (ushort)Species.Manaphy && Style == SpriteStyle.Classic)
            return LoadFromPrefix(folder, "b_490_e.png");

        return LoadFromPrefix(folder, res.EggFile);
    }
```

- [ ] **Step 5: Remove the now-stale shiny comment in `GetSpriteName`**

In `GetSpriteName`, delete the stale comment line (line 170):

```csharp
        // Note: Shiny uses separate folder (ShinyPrefix), not a filename suffix
        return sb.ToString();
```

so it reads:

```csharp
        return sb.ToString();
```

(The shiny `s` suffix is now appended in `GetResourceName`.)

- [ ] **Step 6: Write failing loader behavior tests**

Append to `Tests/PKHeX.Avalonia.Tests/SpriteStyleTests.cs` (inside the class). Add `using SkiaSharp;` and `using System.Linq;` to the file's usings:

```csharp
    [Fact]
    public void Loader_Classic_LoadsKnownSpecies()
    {
        var loader = new SpriteLoader { Style = SpriteStyle.Classic };
        using var bmp = loader.GetSprite(25, 0, 0, 0, false, EntityContext.Gen9);
        Assert.NotNull(bmp);
    }

    [Fact]
    public void Loader_Classic_ShinyDiffersFromNonShiny()
    {
        // Proves the s-suffix fix: b_25s.png loads instead of falling back to b_25.png.
        var loader = new SpriteLoader { Style = SpriteStyle.Classic };
        using var shiny = loader.GetSprite(25, 0, 0, 0, true, EntityContext.Gen9);
        using var normal = loader.GetSprite(25, 0, 0, 0, false, EntityContext.Gen9);
        Assert.NotNull(shiny);
        Assert.NotNull(normal);
        Assert.False(shiny!.Bytes.SequenceEqual(normal!.Bytes));
    }

    [Fact]
    public void Loader_Artwork_LoadsGen9Species()
    {
        var loader = new SpriteLoader { Style = SpriteStyle.Artwork };
        using var bmp = loader.GetSprite(906, 0, 0, 0, false, EntityContext.Gen9); // Sprigatito
        Assert.NotNull(bmp);
    }

    [Fact]
    public void Loader_Classic_MissingGen9Species_ReturnsNull()
    {
        // Classic set has no Gen 9 sprites; loader returns null (renderer then draws a placeholder).
        var loader = new SpriteLoader { Style = SpriteStyle.Classic };
        using var bmp = loader.GetSprite(906, 0, 0, 0, false, EntityContext.Gen9);
        Assert.Null(bmp);
    }
```

- [ ] **Step 7: Run the loader tests**

Run: `dotnet test Tests/PKHeX.Avalonia.Tests/PKHeX.Avalonia.Tests.csproj --filter "FullyQualifiedName~SpriteStyleTests"`
Expected: PASS (all selector + loader tests). The `ShinyDiffersFromNonShiny` test is the key regression guard for the shiny fix.

- [ ] **Step 8: Commit**

```bash
git add PKHeX.Avalonia/Services/SpriteLoader.cs Tests/PKHeX.Avalonia.Tests/SpriteStyleTests.cs
git commit -m "feat(sprites): style-aware SpriteLoader + fix shiny sprite filename suffix"
```

---

### Task 4: Wire the style to the loaded save in AvaloniaSpriteRenderer

**Files:**
- Modify: `PKHeX.Avalonia/Services/AvaloniaSpriteRenderer.cs`

- [ ] **Step 1: Inject AppSettings and set the style on save load**

Replace the field/`Initialize` block (lines 15-21):

```csharp
    private readonly SpriteLoader _loader = new();
    private EntityContext _context = EntityContext.None;

    public void Initialize(SaveFile sav)
    {
        _context = sav.Context;
    }
```

with:

```csharp
    private readonly SpriteLoader _loader = new();
    private readonly AppSettings _settings;
    private EntityContext _context = EntityContext.None;

    public AvaloniaSpriteRenderer(AppSettings settings)
    {
        _settings = settings;
    }

    public void Initialize(SaveFile sav)
    {
        _context = sav.Context;
        _loader.Style = SpriteStyleSelector.Resolve(_settings.Sprite.SpritePreference, sav);
        _loader.ClearCache();
    }
```

(`AppSettings`, `SpriteStyleSelector`, and `SpriteLoader` are all in `PKHeX.Avalonia.Services`, the same namespace — no new `using`.)

- [ ] **Step 2: Build to verify DI still resolves**

Run: `dotnet build PKHeX.Avalonia/PKHeX.Avalonia.csproj -clp:ErrorsOnly`
Expected: Build succeeded. (DI injects the singleton `AppSettings`; no registration change needed.)

- [ ] **Step 3: Commit**

```bash
git add PKHeX.Avalonia/Services/AvaloniaSpriteRenderer.cs
git commit -m "feat(sprites): select sprite style from loaded save in renderer"
```

---

### Task 5: SpritePreference → label converter

**Files:**
- Create: `PKHeX.Avalonia/Converters/SpritePreferenceConverter.cs`

- [ ] **Step 1: Write the converter**

Create `PKHeX.Avalonia/Converters/SpritePreferenceConverter.cs`:

```csharp
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using PKHeX.Avalonia.Services;

namespace PKHeX.Avalonia.Converters;

/// <summary>Maps <see cref="SpritePreference"/> values to user-facing labels (upstream wording).</summary>
public sealed class SpritePreferenceLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SpritePreference.UseSuggested => "Use Suggested",
        SpritePreference.ForceSprites => "Force Sprites",
        SpritePreference.ForceMugshots => "Force Mugshots",
        SpritePreference.ForceArtwork => "Force Artwork",
        _ => value?.ToString(),
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build PKHeX.Avalonia/PKHeX.Avalonia.csproj -clp:ErrorsOnly`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add PKHeX.Avalonia/Converters/SpritePreferenceConverter.cs
git commit -m "feat(sprites): add SpritePreference label converter"
```

---

### Task 6: Surface the preference in SettingsViewModel

**Files:**
- Modify: `PKHeX.Avalonia/ViewModels/SettingsViewModel.cs`

- [ ] **Step 1: Add the property and option list**

After the SlotWrite properties block (line 40, `_modifyUnset`), add:

```csharp
    // Sprites
    [ObservableProperty] private SpritePreference _spritePreference;
    public IReadOnlyList<SpritePreference> SpritePreferences { get; } = Enum.GetValues<SpritePreference>();
```

- [ ] **Step 2: Load it**

In `Load()`, after the `ModifyUnset = ...` line (line 55), add:

```csharp
        SpritePreference = _settings.Sprite.SpritePreference;
```

- [ ] **Step 3: Save it**

In the `Save()` command, after the `_settings.SlotWrite.ModifyUnset = ModifyUnset;` line (line 72), add:

```csharp
        _settings.Sprite.SpritePreference = SpritePreference;
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build PKHeX.Avalonia/PKHeX.Avalonia.csproj -clp:ErrorsOnly`
Expected: Build succeeded. (`SpritePreference` enum is in `PKHeX.Avalonia.Services`, already imported via `using PKHeX.Avalonia.Services;` at the top of the file.)

- [ ] **Step 5: Commit**

```bash
git add PKHeX.Avalonia/ViewModels/SettingsViewModel.cs
git commit -m "feat(sprites): expose sprite preference in SettingsViewModel"
```

---

### Task 7: Add the "Sprites" card to SettingsView

**Files:**
- Modify: `PKHeX.Avalonia/Views/SettingsView.axaml`

- [ ] **Step 1: Declare the converter namespace + resource**

Change the opening `UserControl` tag (lines 1-5) to add the converters namespace:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:PKHeX.Avalonia.ViewModels"
             xmlns:conv="using:PKHeX.Avalonia.Converters"
             x:Class="PKHeX.Avalonia.Views.SettingsView"
             x:DataType="vm:SettingsViewModel">

    <UserControl.Resources>
        <conv:SpritePreferenceLabelConverter x:Key="SpritePrefLabel"/>
    </UserControl.Resources>
```

(The existing `<ScrollViewer>` stays right after this block.)

- [ ] **Step 2: Add the Sprites card**

Insert this `Border` immediately before the "Save Changes" `Button` (currently line 67), inside the outer `StackPanel`:

```xml
            <!-- Sprite Settings Card -->
            <Border Classes="section-card" Padding="20" CornerRadius="8">
                <StackPanel Spacing="16">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock Text="🎨" FontSize="20" VerticalAlignment="Center"/>
                        <TextBlock Text="Sprites" FontSize="18" FontWeight="SemiBold" VerticalAlignment="Center"/>
                    </StackPanel>

                    <Grid ColumnDefinitions="*,Auto">
                        <StackPanel Grid.Column="0">
                            <TextBlock Text="Sprite Style" FontWeight="Medium"/>
                            <TextBlock Text="Which Pokémon sprite set to display. 'Use Suggested' matches the loaded save's game (artwork for Scarlet/Violet, mugshots for Legends: Arceus, classic otherwise)."
                                       FontSize="12" Opacity="0.6" TextWrapping="Wrap"/>
                        </StackPanel>
                        <ComboBox Grid.Column="1" MinWidth="170" VerticalAlignment="Center"
                                  ItemsSource="{Binding SpritePreferences}"
                                  SelectedItem="{Binding SpritePreference}">
                            <ComboBox.ItemTemplate>
                                <DataTemplate x:CompileBindings="False">
                                    <TextBlock Text="{Binding Converter={StaticResource SpritePrefLabel}}"/>
                                </DataTemplate>
                            </ComboBox.ItemTemplate>
                        </ComboBox>
                    </Grid>
                </StackPanel>
            </Border>
```

(`x:CompileBindings="False"` on the item template avoids compiled-binding type friction, since the item is a bare enum value bound through the converter.)

- [ ] **Step 3: Build to verify the XAML compiles**

Run: `dotnet build PKHeX.Avalonia/PKHeX.Avalonia.csproj -clp:ErrorsOnly`
Expected: Build succeeded (Avalonia XAML compiler validates the markup).

- [ ] **Step 4: Commit**

```bash
git add PKHeX.Avalonia/Views/SettingsView.axaml
git commit -m "feat(sprites): add sprite style selector to Settings view"
```

---

### Task 8: Add a public sprite refresh to the editor

**Files:**
- Modify: `PKHeX.Avalonia/ViewModels/PokemonEditorViewModel.cs`

- [ ] **Step 1: Add a public passthrough to `UpdateSprite`**

Immediately above the private `UpdateSprite()` method (line 419), add:

```csharp
    /// <summary>Re-renders the editor preview sprite (e.g. after the sprite style changes).</summary>
    public void RefreshSprite() => UpdateSprite();
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build PKHeX.Avalonia/PKHeX.Avalonia.csproj -clp:ErrorsOnly`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add PKHeX.Avalonia/ViewModels/PokemonEditorViewModel.cs
git commit -m "feat(sprites): expose RefreshSprite on the Pokémon editor"
```

---

### Task 9: Apply preference changes live when Settings closes

**Files:**
- Modify: `PKHeX.Avalonia/ViewModels/MainWindowViewModel.EditorDialogs.cs`

- [ ] **Step 1: Re-initialize the renderer and refresh views after the dialog**

Replace `OpenSettingsAsync` (lines 25-36):

```csharp
    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var vm = new SettingsViewModel(_settings);
        var view = new SettingsView { DataContext = vm };
        vm.CloseRequested += () =>
        {
            var window = TopLevel.GetTopLevel(view) as Window;
            window?.Close();
        };
        await _dialogService.ShowDialogAsync(view, "Settings");
    }
```

with:

```csharp
    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var vm = new SettingsViewModel(_settings);
        var view = new SettingsView { DataContext = vm };
        vm.CloseRequested += () =>
        {
            var window = TopLevel.GetTopLevel(view) as Window;
            window?.Close();
        };
        await _dialogService.ShowDialogAsync(view, "Settings");

        // The sprite preference may have changed; re-apply the style and refresh open views.
        if (CurrentSave is not null)
        {
            _spriteRenderer.Initialize(CurrentSave);
            BoxViewer?.RefreshCurrentBox();
            PartyViewer?.RefreshParty();
            CurrentPokemonEditor?.RefreshSprite();
        }
    }
```

(`CurrentSave`, `BoxViewer`, `PartyViewer`, `CurrentPokemonEditor`, `_spriteRenderer`, and `_dialogService` are all members of `MainWindowViewModel`, accessible from this partial-class file.)

- [ ] **Step 2: Build to verify**

Run: `dotnet build PKHeX.Avalonia/PKHeX.Avalonia.csproj -clp:ErrorsOnly`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add PKHeX.Avalonia/ViewModels/MainWindowViewModel.EditorDialogs.cs
git commit -m "feat(sprites): apply sprite preference changes live from Settings"
```

---

### Task 10: Version bump, full verification, and PR

**Files:**
- Modify: `Directory.Build.props`

- [ ] **Step 1: Bump UIVersion**

In `Directory.Build.props`, change:

```xml
    <UIVersion>1.1.23</UIVersion>
```

to:

```xml
    <UIVersion>1.1.24</UIVersion>
```

- [ ] **Step 2: Full solution build**

Run: `dotnet build PKHeX.sln -clp:ErrorsOnly`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Full test run**

Run: `dotnet test Tests/PKHeX.Avalonia.Tests/PKHeX.Avalonia.Tests.csproj`
Expected: All tests pass (existing suite + new `SpriteStyleTests`). Investigate any regression before proceeding.

- [ ] **Step 4: Manual verification (run the app)**

Launch the app (`dotnet run --project PKHeX.Avalonia/PKHeX.Avalonia.csproj`) and confirm:
- Open a **Scarlet/Violet** save → box/party show **HOME artwork**; a Gen 9 species (e.g. Gholdengo #1000) shows a real sprite, not a `#1000` placeholder.
- Open a **Legends: Arceus** save → sprites are the **circular mugshots**.
- Open a **Gen 4/5/SwSh** save → **classic box sprites** (unchanged from before).
- A **shiny** Pokémon in a classic/LA save now shows the real **shiny sprite** (distinct art) plus the star overlay.
- In **Settings → Sprites**, switch to "Force Artwork" / "Force Mugshots" / "Force Sprites" and Save → the open box/party/editor sprites update **immediately**. "Use Suggested" restores auto-selection.

- [ ] **Step 5: Commit and push**

```bash
git add Directory.Build.props
git commit -m "chore: bump UIVersion to 1.1.24 for generation-aware sprites"
git push -u origin feature/generation-aware-sprites
```

- [ ] **Step 6: Open the PR**

```bash
gh pr create --title "Generation-aware Pokémon sprites (match upstream sprite style per save)" \
  --body "Selects the sprite style based on the loaded save — HOME artwork for Scarlet/Violet & Legends Z-A, circular mugshots for Legends: Arceus, classic box sprites otherwise — mirroring upstream PKHeX. Adds a Settings preference (Use Suggested / Force Sprites / Force Mugshots / Force Artwork) that applies live. Also fixes a latent bug where shiny sprites never loaded (wrong filename — missing 's' suffix). All sprite assets were already bundled; no new assets. Spec & plan under docs/superpowers/. UIVersion 1.1.23 → 1.1.24."
```

---

## Self-review notes

- **Spec coverage:** style selection (Task 1), style-aware loader + shiny fix (Task 3), save wiring (Task 4), settings model/VM/view (Tasks 2/6/7), label wording (Task 5), live apply (Tasks 8/9), tests (Tasks 1/3), edge cases — Gen 9 in classic returns null → placeholder (Task 3 test), shiny in artwork falls back via `ShinyFolder == null` (Task 3), Manaphy egg classic-only (Task 3) — UIVersion bump (Task 10). All spec sections map to a task.
- **Out of scope (per spec):** retro per-gen sprites, an Artwork-shiny set, the `DoNotChange` preference, per-style item sprites (items stay `Big_Items`).
- **Type consistency:** `SpriteStyle{Classic,Mugshot,Artwork}`, `SpritePreference{UseSuggested,ForceSprites,ForceMugshots,ForceArtwork}`, `SpriteStyleSelector.GetSuggested/Resolve`, `SpriteLoader.Style`, `AppSettings.Sprite.SpritePreference`, `SettingsViewModel.SpritePreference/SpritePreferences`, `SpritePreferenceLabelConverter`, `PokemonEditorViewModel.RefreshSprite()` — names used identically across tasks.
