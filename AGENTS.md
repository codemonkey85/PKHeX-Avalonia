# PKHeX-Avalonia

> Canonical instructions for all coding agents (Claude Code, Codex, GitHub Copilot). Claude loads this via the CLAUDE.md stub.

A native Avalonia (11.x) port of [PKHeX](https://github.com/kwsch/PKHeX), the Pokémon save editor —
cross-platform (Windows/macOS/Linux) instead of WinForms-only. Built on .NET 10 + Avalonia 11.x with
CommunityToolkit.MVVM.

Development is AI-assisted (Claude Code, Codex), and this is now publicly disclosed. The `.claude/`
and `.codex/` directories in this repo are real, in-use automation (hooks, skills, subagents) — not
decoration. Treat them as part of the project's tooling.

## Hard Rules

1. **`PKHeX.Core/` is a byte-for-byte upstream mirror** of kwsch/PKHeX. Never edit it manually to make
   something compile — port the fix into the consumer layers (`PKHeX.Application`, `PKHeX.Infrastructure`,
   `PKHeX.Presentation`, `PKHeX.Avalonia`) instead. The only exception is the `chore/sync-pkhex-core-*`
   branch produced by the `sync-upstream-core` skill, which replaces `PKHeX.Core/` wholesale from upstream.
2. **`PKHeX.AutoMod/` is vendored** (the Auto Legality Mod legalization engine from santacrab2/PKHeX-Plugins).
   See `PKHeX.AutoMod/VENDORED.md` for the re-sync procedure — no `.cs` source edits under `AutoMod/` or
   `Enhancements/`; if a Core sync breaks compilation, fix it there and log the change in that file.
3. **No direct pushes to `main`.** Every change is a branch + PR. Enforced by `.claude/hooks/block-main-writes.sh`
   (and the equivalent `.codex/hooks/block-main-writes.sh` for Codex).
4. **Bump `UIVersion`** in `Directory.Build.props` on every PR: `feat` → minor, `fix`/`chore`/`deps`/`refactor`
   → patch, breaking → major. (Top-level `<Version>` tracks upstream PKHeX.Core — not the one to bump.)

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

Patterns to know:
- **`ViewLocator`** (`PKHeX.Avalonia/ViewLocator.cs`) — the single place that maps a dialog ViewModel
  type to its View, via a compile-checked dictionary. It lives in the host so Presentation never
  references Views.
- **`IWindowService.ShowTool`** (`PKHeX.Application/Abstractions/IWindowService.cs`) — opens a modeless
  auxiliary tool window for panels that shouldn't crowd the main editor (e.g. batch search, box report).
  Singleton-per-VM (re-invoke focuses existing).
- **Thin per-gen editors** keep direct Core-block access (not wrapped in interactors).
- **Sprites cross as PNG `byte[]`** — `ISpriteRenderer` → `PngBytesToBitmapConverter` in Views.

### Architecture constraints

- **Application/Infrastructure** stay free of Avalonia, Skia, AND CommunityToolkit.Mvvm (plain
  events/POCOs). CommunityToolkit.Mvvm is allowed only in Presentation.
- **Sprite boundary:** `ISpriteRenderer` returns PNG `byte[]`; host `PngBytesToBitmapConverter`
  materializes the Bitmap in Views.
- **Navigation:** `IDialogService` (framework-free) + `IWindowService.ShowDialogAsync(vm, title)`.
- **`GameInfo.*` statics** (read in 51 files) are Core/Entities reads — left as-is; only language
  mutation is owned by Application LanguageService.
- **Workflow use cases** are stateless and `new`'d at call sites (not DI-injected) to avoid ctor bloat.

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

## Build / Test

```
dotnet build PKHeX.sln -c Release
dotnet test PKHeX.sln -c Release
```

A clean build is expected to produce **0 warnings**. Test projects live under `Tests/`:
`PKHeX.Core.Tests`, `PKHeX.Avalonia.Tests`, `PKHeX.Architecture.Tests`.

- `Tests/PKHeX.Avalonia.Tests/` — xUnit + Avalonia.Headless + Moq
- `Tests/PKHeX.Core.Tests/Legality/Legal/` — 133 legal PKM fixtures
- `Tests/PKHeX.Core.Tests/Legality/Illegal/` — 43 illegal PKM fixtures

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

## Dependency policy

- Stay on latest **11.x Avalonia** and **SkiaSharp 3.x**.
- Avalonia 12 / SkiaSharp 4 deferred — they are major versions with breaking API changes and need
  dedicated, UI-tested PRs. Don't bundle them into routine sweeps.

## Upstream Sync Automation

A daily workflow checks kwsch/PKHeX against `.github/upstream-sync/last-synced-sha.txt` and opens a
`PKHeX.Core Sync Required` issue (labeled `sync`) when upstream has moved. The full sync process —
mirroring Core 1:1, fixing consumer call sites, an Avalonia frontend-parity review of upstream's
WinForms UI changes, version bump, PR, and auto-merge once CI is green — is encoded in
`.claude/skills/sync-upstream-core/SKILL.md`. In detail:
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

## Cross-agent conventions

- This file (`AGENTS.md`) is the single source of truth for agent instructions in this repo. `CLAUDE.md` and `.github/copilot-instructions.md` are pointers to it — never edit them, never duplicate content into them.
- Reusable skills live in `.claude/skills/` (one folder per skill with a `SKILL.md`). GitHub Copilot reads that directory natively; Codex sees it via the `.agents/skills` symlink. New skills always go in `.claude/skills/`.
- Claude-specific subagent definitions live in `.claude/agents/`. If you are not Claude Code, you may read them as role/process guidance.
- Session continuity across tools: before ending substantial work in ANY tool (Claude Code, Codex, Copilot), record durable context — decisions made, gotchas discovered, in-progress state worth resuming — in the "Working notes" section below, or fold it into the relevant section above. This is the shared memory between agents.

## Working notes

<!-- Any agent: append short dated notes here (YYYY-MM-DD — note). Prune notes when stale or once folded into the sections above. -->
