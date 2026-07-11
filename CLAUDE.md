# PKHeX-Avalonia

A native Avalonia (11.x) port of [PKHeX](https://github.com/kwsch/PKHeX), the Pokémon save editor —
cross-platform (Windows/macOS/Linux) instead of WinForms-only.

Development is AI-assisted (Claude Code), and this is now publicly disclosed. The `.claude/` directory
in this repo is real, in-use automation (hooks, skills, subagents) — not decoration. Treat it as part
of the project's tooling.

## Hard Rules

1. **`PKHeX.Core/` is a byte-for-byte upstream mirror** of kwsch/PKHeX. Never edit it manually to make
   something compile — port the fix into the consumer layers (`PKHeX.Application`, `PKHeX.Infrastructure`,
   `PKHeX.Presentation`, `PKHeX.Avalonia`) instead. The only exception is the `chore/sync-pkhex-core-*`
   branch produced by the `sync-upstream-core` skill, which replaces `PKHeX.Core/` wholesale from upstream.
2. **`PKHeX.AutoMod/` is vendored** (the Auto Legality Mod legalization engine from santacrab2/PKHeX-Plugins).
   See `PKHeX.AutoMod/VENDORED.md` for the re-sync procedure — no `.cs` source edits under `AutoMod/` or
   `Enhancements/`; if a Core sync breaks compilation, fix it there and log the change in that file.
3. **No direct pushes to `main`.** Every change is a branch + PR. Enforced by `.claude/hooks/block-main-writes.sh`.
4. **Bump `UIVersion`** in `Directory.Build.props` on every PR: `feat` → minor, `fix`/`chore`/`deps`/`refactor`
   → patch, breaking → major.

## Architecture

Five-project Clean Architecture split (verified against the `.csproj` files):

```
PKHeX.Core            no project references (vendored, upstream mirror)
PKHeX.Application      -> Core                                  (ports/abstractions, no UI deps)
PKHeX.Infrastructure   -> Application, Core, AutoMod             (implementations)
PKHeX.Presentation     -> Application, Core                      (Avalonia-free ViewModels)
PKHeX.Avalonia         -> Core, Application, Infrastructure, Presentation   (host/composition root)
PKHeX.AutoMod          -> Core                                   (vendored ALM engine)
```

Presentation depends on Application + Core only — it does **not** reference Infrastructure, so
ViewModels stay free of both Avalonia and implementation details. Avalonia is the only project that
references all four and is where DI wiring and Views live. This is enforced by
`Tests/PKHeX.Architecture.Tests/LayerDependencyTests.cs` (NetArchTest-based).

Two patterns to know:
- **`ViewLocator`** (`PKHeX.Avalonia/ViewLocator.cs`) — the single place that maps a dialog ViewModel
  type to its View, via a compile-checked dictionary. It lives in the host so Presentation never
  references Views.
- **`IWindowService.ShowTool`** (`PKHeX.Application/Abstractions/IWindowService.cs`) — opens a modeless
  auxiliary tool window for panels that shouldn't crowd the main editor (e.g. batch search, box report).

## Build / Test

```
dotnet build PKHeX.sln -c Release
dotnet test PKHeX.sln -c Release
```

A clean build is expected to produce **0 warnings**. Test projects live under `Tests/`:
`PKHeX.Core.Tests`, `PKHeX.Avalonia.Tests`, `PKHeX.Architecture.Tests`.

## Guardrail Tests (fail a PR if ignored)

- **`Tests/PKHeX.Avalonia.Tests/AccessibilityAuditTests.cs`** — regex-scans every `.axaml` view for
  icon-only interactive controls (`Button`/`ToggleButton`/`RepeatButton` with no visible text) and
  requires `AutomationProperties.Name`. Justified exceptions go in `accessibility-allowlist.txt` next
  to the test.
- **`Tests/PKHeX.Avalonia.Tests/LocalizationAuditTests.cs`** — regex-scans `.axaml` views and
  ViewModels for hardcoded user-facing English string literals instead of `{loc:Loc Key}` /
  `LocalizedStrings`. New/migrated files are enforced by default; the not-yet-migrated backlog is
  listed in `localization-allowlist.txt`.
- **`Tests/PKHeX.Architecture.Tests/LayerDependencyTests.cs`** — enforces the project reference
  direction above (e.g. Application must not depend on Avalonia/Infrastructure/Presentation).

## Localization

Resource files live in `PKHeX.Presentation/Localization/Strings/` (`LocalizedStrings.cs` / `LocExtension.cs`
drive lookup) with one JSON file per language: **9 languages** — `de`, `en`, `es`, `fr`, `it`, `ja`, `ko`,
`zh-Hans`, `zh-Hant`. Any new user-facing string needs a key added to **all 9** files, not just `en.json`.

## Theming

Theming is driven by `IThemeService` (`PKHeX.Application/Abstractions/IThemeService.cs`, `AppTheme` enum)
and implemented in `PKHeX.Avalonia/Services/ThemeService.cs` using Avalonia's `ThemeVariant`/
`ThemeDictionaries` APIs (`PKHeX.Avalonia/Styles/Theme.axaml`), including tracking the OS light/dark
preference for the `System` option. Covered by `Tests/PKHeX.Avalonia.Tests/ThemeTests.cs`.

## Upstream Sync Automation

A daily workflow checks kwsch/PKHeX against `.github/upstream-sync/last-synced-sha.txt` and opens a
`PKHeX.Core Sync Required` issue (labeled `sync`) when upstream has moved. The full sync process —
mirroring Core 1:1, fixing consumer call sites, an Avalonia frontend-parity review of upstream's
WinForms UI changes, version bump, PR, and auto-merge once CI is green — is encoded in
`.claude/skills/sync-upstream-core/SKILL.md`.
