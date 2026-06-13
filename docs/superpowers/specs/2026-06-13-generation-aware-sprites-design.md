# Generation-aware Pokémon Sprites

**Date:** 2026-06-13
**Branch:** `feature/generation-aware-sprites`
**Status:** Design — pending implementation

## Problem

PKHeX-Avalonia always renders Pokémon using the **classic box sprite** set (`b_*`,
"Big Pokemon Sprites"), regardless of which save file is open. Upstream PKHeX instead
selects one of **three** bundled sprite styles based on the loaded save, so a
Scarlet/Violet save shows HOME-style artwork and a Legends: Arceus save shows circular
mugshots. This fork already bundles all three sprite sets on disk
(`Artwork Pokemon Sprites/`, `Legends Arceus Sprites/`), but the loader never selects
them — the artwork (`a_*`) and Legends-Arceus (`c_*`) sets sit unused.

This work makes the app match upstream's sprite-style selection.

## Goal

When a save is loaded, pick the sprite style the way upstream does, and add a user
setting to override the auto-selection. This is a faithful port of upstream's
`SpriteBuilderUtil.GetSuggestedMode` + `SpriteBuilderPreference`, adapted to this fork's
SkiaSharp `SpriteLoader`.

## Non-goals

- **True retro per-generation sprites** (GameBoy Gen 1/2, RSE Gen 3, DPPt Gen 4, etc.).
  Those asset sets do not exist upstream or in this repo. Out of scope.
- Adding a separate **Artwork Shiny** sprite set. Not bundled in this fork; shiny in
  Artwork mode falls back gracefully (see Shiny handling).
- Upstream's `DoNotChange` preference value (keep the currently-active style across save
  switches). Niche; omitted for simplicity. Can be added later if requested.

## Background: how upstream selects the style

Upstream lives in `PKHeX.Drawing.PokeSprite` (NOT `PKHeX.Core`). It keeps a single active
`SpriteBuilder` (`SpriteUtil.Spriter`) chosen per loaded save:

| Loaded save | Suggested mode | Resource set | Filename letter |
|---|---|---|---|
| `SAV8LA` (Legends: Arceus) | Circle mugshot | `Legends Arceus Sprites` | `c` |
| `SAV9SV` / `SAV9ZA` (Scarlet/Violet, Legends Z-A) | Artwork | `Artwork Pokemon Sprites` | `a` |
| everything else | Classic box | `Big Pokemon Sprites` | `b` |

A user setting (`SpriteBuilderPreference`) can force a specific style instead of using the
suggested one. All three sprite sets in this fork are **68×56**, so no canvas-dimension
changes are required.

Because upstream keeps the 1:1 sprite logic out of `PKHeX.Core`, the port lives entirely
in the Avalonia layer — consistent with the project rule that `PKHeX.Core` stays 1:1 with
upstream.

## Design

### 1. New types (Avalonia layer)

```
PKHeX.Avalonia/Services/SpriteStyle.cs          (new)
PKHeX.Avalonia/Services/SpriteStyleSelector.cs  (new)
```

`SpriteStyle` (resolved style used by the loader):

```csharp
public enum SpriteStyle { Classic, Mugshot, Artwork }
```

`SpritePreference` (user setting; upstream wording for labels):

```csharp
public enum SpritePreference { UseSuggested, ForceSprites, ForceMugshots, ForceArtwork }
```

`SpriteStyleSelector` — static, mirrors upstream's save-type switch exactly:

```csharp
public static SpriteStyle GetSuggested(SaveFile sav) => sav switch
{
    SAV8LA            => SpriteStyle.Mugshot,
    SAV9SV or SAV9ZA  => SpriteStyle.Artwork,
    _                 => SpriteStyle.Classic,
};

public static SpriteStyle Resolve(SpritePreference pref, SaveFile sav) => pref switch
{
    SpritePreference.ForceSprites   => SpriteStyle.Classic,
    SpritePreference.ForceMugshots  => SpriteStyle.Mugshot,
    SpritePreference.ForceArtwork   => SpriteStyle.Artwork,
    _                               => GetSuggested(sav), // UseSuggested
};
```

### 2. `SpriteLoader` becomes style-aware

File: `PKHeX.Avalonia/Services/SpriteLoader.cs`

- Add `public SpriteStyle Style { get; set; } = SpriteStyle.Classic;`
- Replace the two hardcoded constants (`SpritePrefix`, `ShinyPrefix`) with a per-style
  descriptor resolved from `Style`, e.g.:

  | Style | normal folder | shiny folder | letter | egg file | has shiny set |
  |---|---|---|---|---|---|
  | Classic | `Big_Pokemon_Sprites` | `Big_Shiny_Sprites` | `b` | `b_egg.png` | yes |
  | Mugshot | `Legends_Arceus_Sprites` | `Legends_Arceus_Shiny_Sprites` | `c` | `c_egg.png` | yes |
  | Artwork | `Artwork_Pokemon_Sprites` | (none) | `a` | `a_egg.png` | no |

  (Resource namespaces use underscores for the spaces in folder names.)
- `GetResourceName(...)` selects the folder/letter from the active style descriptor.
- **Shiny suffix fix:** the bundled shiny files carry an `s` suffix (`b_25s.png`,
  `c_25s.png`), but the current code looks up `b_25.png` inside the shiny folder, so shiny
  sprites never load (everything falls back to non-shiny + a drawn gold star). Fix: when
  the style has a shiny set and `shiny` is requested, the filename is
  `<letter><spriteName>s.png` in the shiny folder. The existing fallback chain
  (shiny → non-shiny → base form → species-only) is preserved.
- **Artwork shiny:** Artwork has no shiny set. A shiny request in Artwork mode resolves to
  the non-shiny artwork name; the shiny-star overlay (added in `ComposeSprite`) still
  marks it shiny. This relies on the existing shiny→non-shiny fallback.
- The species-only ultimate fallback (currently hardcoded `b_<species>.png`) and
  `GetEggSprite` use the active style's letter/egg file. Manaphy's special egg
  (`b_490_e.png`) exists only in the classic set, so keep that special case for Classic
  only; other styles use the generic style egg.

### 3. Wiring to the loaded save

File: `PKHeX.Avalonia/Services/AvaloniaSpriteRenderer.cs`

- Inject the existing `AppSettings` singleton via the constructor (DI already registers it
  in `App.axaml.cs`).
- In `Initialize(SaveFile sav)`:
  - `_loader.Style = SpriteStyleSelector.Resolve(_settings.Sprite.SpritePreference, sav);`
  - `_loader.ClearCache();`
  - keep storing `_context = sav.Context;` (still used for the Pikachu-cosplay detail).

Because `AvaloniaSpriteRenderer` is a DI singleton used by every viewer (box, party,
editor, encounter/gift databases, hall of fame, etc.), they all render in the active
save's style automatically — matching upstream's single-active-builder model. No per-call
signature changes are required; the per-call `EntityContext` parameter stays for the
existing Pikachu special-case.

### 4. User setting

**Model** — `PKHeX.Avalonia/Services/AppSettings.cs`:
- Add a `SpriteSettings` group:
  ```csharp
  public class SpriteSettings
  {
      public SpritePreference SpritePreference { get; set; } = SpritePreference.UseSuggested;
  }
  ```
- Add `[ObservableProperty] private SpriteSettings _sprite = new();` alongside the other
  groups, so it serializes to `config.json`.

**ViewModel** — `PKHeX.Avalonia/ViewModels/SettingsViewModel.cs`:
- Add `[ObservableProperty] private SpritePreference _spritePreference;`
- Add `public IReadOnlyList<SpritePreference> SpritePreferences { get; } =
  Enum.GetValues<SpritePreference>();`
- Load/Save it alongside the existing settings.

**View** — `PKHeX.Avalonia/Views/SettingsView.axaml`:
- Add a "Sprites" `section-card` (matching the existing card style) containing a
  `ComboBox` bound to `SpritePreference` / `SpritePreferences`. Labels shown to the user
  use upstream wording: **Use Suggested**, **Force Sprites**, **Force Mugshots**,
  **Force Artwork**. Because the raw enum names have no spaces (`UseSuggested`), the
  ComboBox `ItemTemplate` uses a small `IValueConverter`
  (`SpritePreference` → friendly string) so the four upstream labels render correctly.
  The converter lives in `PKHeX.Avalonia/Converters/` next to the existing converters.

**Live apply** — `PKHeX.Avalonia/ViewModels/MainWindowViewModel.EditorDialogs.cs`:
- After the Settings dialog closes in `OpenSettingsAsync`, if `CurrentSave is not null`:
  - `_spriteRenderer.Initialize(CurrentSave);`
  - refresh open views: `BoxViewer?.RefreshCurrentBox();`,
    `PartyViewer?.RefreshParty();`, and `CurrentPokemonEditor?.RefreshSprite();`.
    `PokemonEditorViewModel` currently has a private `UpdateSprite()` (line ~419); add a
    thin public `RefreshSprite()` that calls it so the open editor preview re-renders.
- This makes a preference change take effect immediately without reloading the save.
  Transient dialogs (encounter/gift DB) already read the current style when opened.

## Data flow

```
save loaded ─► MainWindowViewModel.OnSaveFileChanged
                 └─► AvaloniaSpriteRenderer.Initialize(sav)
                       ├─ style = SpriteStyleSelector.Resolve(pref, sav)
                       ├─ _loader.Style = style
                       └─ _loader.ClearCache()

render ─► AvaloniaSpriteRenderer.GetSprite(pk)
            └─► SpriteLoader.GetSprite(species, form, …)   // uses _loader.Style
                  └─ GetResourceName → folder + letter + (s suffix if shiny set)
                  └─ load embedded PNG (cached), fallback chain, compose overlays
```

## Error / edge handling

- **Missing sprite for a style** (e.g. a species absent from the Artwork set): the existing
  fallback chain (shiny→non-shiny→base form→species-only) applies; if all fail, the
  type-colored placeholder with `#<species>` is drawn. No crash.
- **Shiny in Artwork mode:** non-shiny artwork + star overlay (documented above).
- **Style switch between saves:** cache keys include the folder namespace, so stale entries
  can't collide; `ClearCache()` on `Initialize` keeps memory bounded.
- **Eggs:** style-appropriate egg sprite; Manaphy special egg only for Classic.

## Testing

- **Unit tests** (`Tests/`) for `SpriteStyleSelector`:
  - `GetSuggested` returns Mugshot for `SAV8LA`, Artwork for `SAV9SV`/`SAV9ZA`, Classic
    otherwise (use blank saves via the existing save-construction helpers).
  - `Resolve` honors each Force value regardless of save type, and falls back to
    `GetSuggested` for `UseSuggested`.
- **Loader behavior** can be checked with a lightweight test that resolves resource names
  per style and asserts they exist among the assembly's manifest resource names (verifies
  the `s` shiny suffix fix and per-style folders point at real bundled assets).
- **Manual verification:** load an SV save (artwork), a Legends: Arceus save (mugshots),
  and a Gen 4/5 save (classic); toggle the preference in Settings and confirm sprites
  update live, including a shiny (real shiny sprite now loads in Classic/Mugshot modes).

## Housekeeping

- Bump `UIVersion` +1 patch in `Directory.Build.props` (project rule: every user-facing PR).
- Work stays on `feature/generation-aware-sprites`; open a PR (no direct pushes to `main`).

## Files touched

| File | Change |
|---|---|
| `PKHeX.Avalonia/Services/SpriteStyle.cs` | new — `SpriteStyle`, `SpritePreference` enums |
| `PKHeX.Avalonia/Services/SpriteStyleSelector.cs` | new — suggested/resolve logic |
| `PKHeX.Avalonia/Services/SpriteLoader.cs` | style-aware folders/letter; shiny suffix fix; style egg |
| `PKHeX.Avalonia/Services/AvaloniaSpriteRenderer.cs` | inject `AppSettings`; set style in `Initialize` |
| `PKHeX.Avalonia/Services/AppSettings.cs` | new `SpriteSettings` group |
| `PKHeX.Avalonia/ViewModels/SettingsViewModel.cs` | preference property + Load/Save |
| `PKHeX.Avalonia/Views/SettingsView.axaml` | "Sprites" card with ComboBox |
| `PKHeX.Avalonia/Converters/SpritePreferenceConverter.cs` | new — enum → friendly label |
| `PKHeX.Avalonia/ViewModels/PokemonEditorViewModel.cs` | public `RefreshSprite()` wrapper |
| `PKHeX.Avalonia/ViewModels/MainWindowViewModel.EditorDialogs.cs` | live-apply after Settings closes |
| `Tests/` | `SpriteStyleSelector` unit tests |
| `Directory.Build.props` | bump `UIVersion` |
```