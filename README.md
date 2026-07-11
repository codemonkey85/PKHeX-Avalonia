<div align="center">

# PKHeX Avalonia

![License](https://img.shields.io/badge/License-GPLv3-blue.svg)
![CI](https://github.com/realgarit/PKHeX-Avalonia/actions/workflows/ci.yml/badge.svg)
![Release](https://img.shields.io/github/v/release/realgarit/PKHeX-Avalonia?label=Latest%20Release)
![Latest tag](https://img.shields.io/github/v/tag/realgarit/PKHeX-Avalonia?label=Latest%20Tag)
![Downloads](https://img.shields.io/github/downloads/realgarit/PKHeX-Avalonia/total?label=Downloads)

PKHeX Avalonia is a cross-platform port of [PKHeX](https://github.com/kwsch/PKHeX), the classic
Pokémon save editor, built with Avalonia so it runs on **Windows**, **macOS**, and **Linux**.

</div>

## Download

Get the latest build for your platform from the [Releases](https://github.com/realgarit/PKHeX-Avalonia/releases/latest) page:

| Platform | File |
|----------|------|
| Windows (x64) | `PKHeX-Avalonia-win-x64.zip`, or the `PKHeX-Avalonia-Setup.exe` installer |
| Linux (x64) | `PKHeX-Avalonia-linux-x64.zip`, or `PKHeX-Avalonia-linux-x64.AppImage` |
| macOS Apple Silicon | `PKHeX-Avalonia-osx-arm64.zip`, or `PKHeX-Avalonia-osx-arm64.dmg` |
| macOS Intel | `PKHeX-Avalonia-osx-x64.zip`, or `PKHeX-Avalonia-osx-x64.dmg` |

Every build is self-contained, so you don't need to install .NET. `.dmg` and the Windows installer
are additive alongside the existing `.zip`/`.AppImage` artifacts.

**Signing note:** the installer/dmg builds are code-signed and (on macOS)
notarized only once the repo owner has configured signing secrets — see
[`docs/packaging.md`](docs/packaging.md). Until then, filenames ending in
`-unsigned` (e.g. `PKHeX-Avalonia-Setup-unsigned.exe`,
`PKHeX-Avalonia-osx-arm64-unsigned.dmg`) mean the OS will still warn on
first launch:
- **Windows:** SmartScreen "unknown publisher" — click **More info** then
  **Run anyway**.
- **macOS:** right-click the app, pick **Open**, then click **Open** in the
  dialog (or run `xattr -d com.apple.quarantine
  ~/Downloads/PKHeX.Avalonia.app`).

Package-manager installs (Homebrew cask, winget) are templated under
`packaging/` and documented in `docs/packaging.md`, pending signed release
builds to submit.

The app checks GitHub Releases for updates on startup and shows a changelog after upgrading — see
[`docs/features.md`](docs/features.md#in-app-update-checker).


## Project Structure

The code is split into layers so the UI stays separate from the PKHeX logic:

| Project | What it does | Uses |
|---------|--------------|------|
| **PKHeX.Core** | Save, entity, and legality logic. Kept 1:1 with [upstream PKHeX](https://github.com/kwsch/PKHeX). We don't change it directly. | None |
| **PKHeX.Application** | Use-cases and service interfaces on top of Core. | Core |
| **PKHeX.Infrastructure** | File access, settings, backups, update checks, LiveHeX networking, and other OS bits. | Application, Core |
| **PKHeX.Presentation** | View-models and localization. No UI framework here. | Application, Core |
| **PKHeX.Avalonia** | The Avalonia UI: views, styles, themes, and the desktop app. | all of the above |
| **PKHeX.AutoMod** | Vendored Auto-Legality Mod legalization engine. | Core |

Tests live under `Tests/`: `PKHeX.Core.Tests`, `PKHeX.Avalonia.Tests`, and `PKHeX.Architecture.Tests`
(which checks the layers above stay separate). Full layer map, dependency rules, and vendoring
policy: [`docs/development.md`](docs/development.md).

## Features

### Save editing

* Edit saves from Gen 1 to Gen 9, plus Let's Go, Legends: Arceus, BDSP, and Legends: Z-A.
* Edit any Pokémon: stats, moves, ribbons, memories, and more.
* Checks legality as you go and can fix illegal Pokémon for you.
* Import and export Pokémon files and Showdown sets.
* Move Pokémon between generations. It converts the format for you.
* Search your boxes with the PKM, Mystery Gift, and Encounter databases.
* Edit many Pokémon at once with the batch editor.
* Game specific editors under Tools, like Pokédex, Hall of Fame, and Secret Base.

### App experience

* **Themes:** Dark, Light, High Contrast, and Follow System, switchable at runtime — no restart.
* **Localization:** the app shell is translated into 9 languages (English, Japanese, Korean,
  French, Italian, German, Spanish, Simplified Chinese, Traditional Chinese), switchable live from
  the Options menu. [Contribute a translation.](CONTRIBUTING.md)
* **OS drag-and-drop:** drag a box/party slot out to Finder/Explorer to get an entity file, drop
  entity files onto a slot (or several onto a box to fill it), and drop a save file anywhere on the
  window to open it. [Details below.](#os-drag-and-drop-notes)
* **Accessibility:** keyboard-navigable box/party grids, accessible names on icon-only controls,
  and visible focus in every theme. See [`docs/accessibility.md`](docs/accessibility.md).
* **In-app update checker:** checks GitHub Releases on startup and shows a changelog after
  upgrading.
* **Save backup manager & diff:** automatic timestamped backups of every save you open or write,
  with a restore UI and a slot-by-slot diff between two saves (Tools → Backup Manager).
* **Whole-save legality audit:** a tool window that runs the legality checker over every Pokémon in
  the save at once (Tools → Data → Legality Audit).
* **Platform-standard config/data directories,** with automatic one-time migration from the old
  next-to-executable location.

### Beyond WinForms parity

Tools that go past what the original WinForms PKHeX offers — full guide in
[`docs/features.md`](docs/features.md):

* **Auto-Legality Mod:** paste a Pokémon Showdown set and get back a legal, ready-to-inject
  Pokémon, using the same legalization engine as the original Auto Legality Mod plugin
  (Tools → Showdown → Import Set, `Ctrl+T`).
* **Living Dex generator:** fills an entire Pokédex's worth of boxes with legal Pokémon using that
  same engine (Tools → Generate Living Dex).
* **LiveHeX:** read and write Pokémon directly on a running Nintendo Switch over Wi-Fi via
  [sys-botbase](https://github.com/olliz0r/sys-botbase) — no save export/import round-trip needed
  (Tools → LiveHeX). Requires a hackable/CFW console running sys-botbase.

### OS drag-and-drop notes

* **Drag out (box/party slot → desktop)** writes a decrypted entity file (e.g. `.pk9`) to a temp
  location and hands the OS a real file reference via Avalonia's `IStorageProvider`. This is a
  desktop-backed capability: it works on Windows, macOS, and Linux (X11 and Wayland) when running
  as a normal desktop app. If a future Avalonia backend can't resolve a real file path for the
  temp file (e.g. a sandboxed or browser-hosted build), drag-out degrades gracefully to in-app-only
  dragging (box ↔ party still works) instead of failing.
* **Drag in** accepts `.pk1`–`.pk9`, `.pb7`/`.pb8`, `.pa8`, encrypted `.ek*`, and Mystery Gift files,
  reusing the same detection/conversion/legality pipeline as the existing folder import feature.
  Incompatible or unreadable files are rejected with a message dialog rather than crashing or
  silently corrupting a slot.
* **Drag a save file** onto any part of the main window (a slot, the editor panel, or elsewhere)
  to open it, the same as File > Open.


## Building from Source

### Requirements
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Run
```bash
dotnet run --project PKHeX.Avalonia
```

### Build
```bash
dotnet build PKHeX.sln -c Release
```

### Test
```bash
dotnet test PKHeX.sln
```

### Publish (example: macOS ARM)
```bash
dotnet publish PKHeX.Avalonia -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

See [`docs/development.md`](docs/development.md) for the full layer map, the PKHeX.Core 1:1 sync
policy, and the test suite overview.

## Documentation

* [`docs/features.md`](docs/features.md) — guide to Auto-Legality Mod, Living Dex, LiveHeX, backup
  manager, legality audit, themes, localization, and more.
* [`docs/development.md`](docs/development.md) — build/test/run, Clean Architecture layer map,
  PKHeX.Core sync policy, UIVersion convention, test suite overview.
* [`docs/accessibility.md`](docs/accessibility.md) — keyboard shortcuts and screen-reader notes.
* [`docs/packaging.md`](docs/packaging.md) — how release installers/packages are built and signed.
* [`CONTRIBUTING.md`](CONTRIBUTING.md) — how to contribute a UI translation.
* [`docs/README.md`](docs/README.md) — full documentation index.

## Screenshots

Pokémon editor and box view. The full editor next to the sprite box grid.
![Pokémon editor and box view](docs/screenshots/pokemon-editor.png)
Inventory editor. Edit items by pouch (Medicine, Balls, Berries, Mega Stones, and so on).
![Inventory editor](docs/screenshots/inventory-editor.png)
PKM Database. Search your boxes with a filter rail you can resize or hide.
![PKM Database](docs/screenshots/pkm-database.png)
Save editors. Gen 1 to 9 plus game specific tools under Tools, then Save Editors.
![Save editors menu](docs/screenshots/save-editors-menu.png)

## Credits
This fork is built on the work of the [PKHeX team](https://github.com/kwsch/PKHeX).

* **Logic & Research:** [PKHeX](https://github.com/kwsch/PKHeX)
* **Auto-Legality Mod:** [PKHeX-Plugins](https://github.com/santacrab2/PKHeX-Plugins) by architdate, santacrab2, and contributors (see [`PKHeX.AutoMod/VENDORED.md`](PKHeX.AutoMod/VENDORED.md))
* **LiveHeX protocol:** [sys-botbase](https://github.com/olliz0r/sys-botbase) by olliz0r
* **QR Codes:** [QRCoder](https://github.com/codebude/QRCoder) (MIT)
* **Sprites:** [pokesprite](https://github.com/msikma/pokesprite) (MIT)
* **Arceus Sprites:** National Pokédex Icon Dex project and contributors.
