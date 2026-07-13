# PKHeX-Avalonia — Copilot Instructions

## Project overview

Cross-platform Pokémon save editor built on .NET 10 + Avalonia 11.x with CommunityToolkit.MVVM.
Five-project Clean Architecture split:

```
PKHeX.Core            — no project references (vendored, upstream mirror)
PKHeX.Application      → Core                                (ports/abstractions, no UI deps)
PKHeX.Infrastructure   → Application, Core, AutoMod           (implementations)
PKHeX.Presentation     → Application, Core                    (Avalonia-free ViewModels)
PKHeX.Avalonia         → Core, Application, Infrastructure, Presentation  (host/composition root)
PKHeX.AutoMod          → Core                                  (vendored ALM engine)
```

Presentation depends only on Application + Core — it does **not** reference Infrastructure.
Avalonia is the only project that references all four. This is enforced by
`Tests/PKHeX.Architecture.Tests/LayerDependencyTests.cs` (NetArchTest-based).

### Key patterns
- **ViewLocator** (`PKHeX.Avalonia/ViewLocator.cs`) — single place mapping dialog ViewModel types
  to Views via a compile-checked dictionary, so Presentation never references Views.
- **IWindowService.ShowTool** (`PKHeX.Application/Abstractions/IWindowService.cs`) — opens modeless
  auxiliary tool windows (e.g. batch search, box report). Singleton-per-VM (re-invoke focuses existing).
- **Thin per-gen editors** keep direct Core-block access (not wrapped in interactors).
- **Sprites cross as PNG `byte[]`** — `ISpriteRenderer` → `PngBytesToBitmapConverter` in Views.

## Hard rules

1. **PKHeX.Core is a byte-for-byte upstream mirror** of kwsch/PKHeX. Never edit it manually to make
   something compile — port the fix into consumer layers (`PKHeX.Application`, `PKHeX.Infrastructure`,
   `PKHeX.Presentation`, `PKHeX.Avalonia`) instead. The exception is the `chore/sync-pkhex-core-*`
   branch produced by the upstream sync process.
2. **PKHeX.AutoMod is vendored** — no `.cs` source edits under `AutoMod/` or `Enhancements/`.
3. **No direct pushes to `main`.** Every change is a branch + PR.
4. **Bump `UIVersion`** in `Directory.Build.props` on every PR: `feat` → minor, `fix`/`chore`/`deps`/`refactor`
   → patch, breaking → major. (Top-level `<Version>` tracks upstream PKHeX.Core — not the one to bump.)

## Workflow

### Branch + PR flow
- Work in feature branches, commit there, push, and open a PR. Never `git push origin main`.
- A clean build is expected to produce **0 warnings**.

### Auto-merge policy
- Claude-created PRs are automatically merged once CI/checks pass — no manual check-in needed.
- Flow: `gh pr checks <n> --watch`, then `gh pr merge <n> --merge --delete-branch`, then delete
  local branch and switch back to main.

### Worktree shipping
- Git commit/push/PR must run from inside the agent's worktree (not the repo root).
- The `block-main-writes.sh` hook checks the shell's cwd branch — so shipping commands must come
  from a Bash session whose cwd is inside the feature-branch worktree.
- Bulk temp data goes in a temp dir inside the worktree on real disk, never on tmpfs (ENOSPC risk).

## Architecture constraints

- **Application/Infrastructure** stay free of Avalonia, Skia, AND CommunityToolkit.Mvvm (plain
  events/POCOs). CommunityToolkit.Mvvm is allowed only in Presentation.
- **Sprite boundary:** `ISpriteRenderer` returns PNG `byte[]`; host `PngBytesToBitmapConverter`
  materializes the Bitmap in Views.
- **Navigation:** `IDialogService` (framework-free) + `IWindowService.ShowDialogAsync(vm, title)`.
- **`GameInfo.*` statics** (read in 51 files) are Core/Entities reads — left as-is; only language
  mutation is owned by Application LanguageService.
- **Workflow use cases** are stateless and `new`'d at call sites (not DI-injected) to avoid ctor bloat.

## Testing

- `dotnet test PKHeX.sln -c Release`
- `Tests/PKHeX.Avalonia.Tests/` — xUnit + Avalonia.Headless + Moq
- `Tests/PKHeX.Core.Tests/Legality/Legal/` — 133 legal PKM fixtures
- `Tests/PKHeX.Core.Tests/Legality/Illegal/` — 43 illegal PKM fixtures
- `Tests/PKHeX.Architecture.Tests/LayerDependencyTests.cs` — enforces Clean Architecture layering

### Guardrail tests
- **AccessibilityAuditTests.cs** — regex-scans every `.axaml` view for icon-only interactive controls
  missing `AutomationProperties.Name`. Justified exceptions in `accessibility-allowlist.txt`.
- **LocalizationAuditTests.cs** — regex-scans for hardcoded user-facing English strings instead of
  `{loc:Loc Key}` / `LocalizedStrings`. Backlog in `localization-allowlist.txt`.
- **LayerDependencyTests.cs** — enforces project reference direction.

## Localization

Resource files in `PKHeX.Presentation/Localization/Strings/` (`LocalizedStrings.cs` / `LocExtension.cs`).
9 languages: `de`, `en`, `es`, `fr`, `it`, `ja`, `ko`, `zh-Hans`, `zh-Hant`. Any new user-facing
string needs a key in **all 9** files, not just `en.json`.

## Theming

`IThemeService` (`PKHeX.Application/Abstractions/IThemeService.cs`, `AppTheme` enum) implemented in
`PKHeX.Avalonia/Services/ThemeService.cs` using `ThemeVariant`/`ThemeDictionaries`
(`PKHeX.Avalonia/Styles/Theme.axaml`), including tracking OS light/dark preference for `System` option.

## Dependency policy

- Stay on latest **11.x Avalonia** and **SkiaSharp 3.x**.
- Avalonia 12 / SkiaSharp 4 deferred — they are major versions with breaking API changes and need
  dedicated, UI-tested PRs. Don't bundle them into routine sweeps.

## Upstream sync

A daily workflow checks kwsch/PKHeX against `.github/upstream-sync/last-synced-sha.txt` and opens a
`PKHeX.Core Sync Required` issue (labeled `sync`) when upstream has moved. The sync process:
1. Fetch the latest PKHeX.Core SHA from kwsch/PKHeX
2. Branch `chore/sync-pkhex-core-<short7>`; mirror `PKHeX.Core/` 1:1
3. Fix broken call sites in consumers only (never in Core)
4. Bump UIVersion; write synced SHA to `last-synced-sha.txt`
5. Check for frontend parity gaps — classify upstream non-Core commits; open `frontend-parity`
   issues for genuine gaps without blocking the Core auto-merge
6. Verify build (0 warn/0 err) + tests + diff=0; open PR; auto-merge once CI is green

## Style preferences

- **Clean architecture over shims** — for new features/integrations, prefer the cleanest
  architecture-correct solution even if it needs a rewrite, over expedient hacks.
- **No planning docs in repo** — don't commit AI-planning artifacts (specs/plans) to git history
  or GitHub. If committed, rewrite them out of branch history before pushing.
- **Prefer clean solution** — lead with the architecture-correct design, not expedient options.

## Known bugs fixed (reference)

- MemoryEditorViewModel.Save(): HT memory feeling/intensity written from OT values (copy-paste bug)
- PokemonEditorViewModel.LoadFromPKM(): Premature Validate() before memory fields loaded
- MainWindowViewModel: BatchEditor.BatchEditCompleted event leak on save close
- PartyViewerViewModel: redundant always-true pattern check
- Various dead fields and redundant OnPropertyChanged calls

## UI testing (computer-use)

- Publish the .app bundle: `dotnet publish PKHeX.Avalonia/PKHeX.Avalonia.csproj -c Debug -r osx-arm64 --self-contained -o <dir>`
- Open with: `open <dir>/PKHeX.Avalonia.app`
- Grant accessibility by bundle ID: `io.pkhex.avalonia`
- Test saves load from `Tests/savefiles/` via File > Open (app does NOT accept CLI file-path arg)
- Re-screenshot before each click — window may shift between actions

## Modeless tool-window pattern

- `IWindowService.ShowTool(vm, title)` + `CloseAllTools()` for auxiliary panels
- Singleton-per-VM (re-invoke focuses existing)
- Remembers size/position per VM type for the session via static `ToolBounds` dict
- `MainWindowViewModel.OnSaveFileChanged` calls `CloseAllTools()`
- First consumer: box seek (`EntitySeekViewModel` + `IBoxNavigator`)

## Automation tooling

- `.claude/` hooks, skills, and agents are committed to the repo (AI assistance publicly disclosed).
- `.claude/worktrees/`, `.claude/settings.local.json`, `.claude/scheduled_tasks.lock` stay gitignored.
- Changes to `.claude/` content go through branch + PR like everything else.