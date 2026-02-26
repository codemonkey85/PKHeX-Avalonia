# PKHeX Avalonia

![License](https://img.shields.io/badge/License-GPLv3-blue.svg)
![CI](https://github.com/realgarit/PKHeX-Avalonia/actions/workflows/ci.yml/badge.svg)
![Release](https://img.shields.io/github/v/release/realgarit/PKHeX-Avalonia?label=Latest%20Release)

PKHeX Avalonia is the cross-platform [PKHeX](https://github.com/kwsch/PKHeX) port built with the Avalonia UI framework, bringing the classic Pokémon save editor to **Windows**, **macOS**, and **Linux** with a native look and feel.

---

## Download

Grab the latest release for your platform from the [Releases](https://github.com/realgarit/PKHeX-Avalonia/releases/latest) page:

| Platform | File |
|----------|------|
| Windows (x64) | `PKHeX-Avalonia-win-x64.zip` |
| Linux (x64) | `PKHeX-Avalonia-linux-x64.zip` |
| macOS Apple Silicon | `PKHeX-Avalonia-osx-arm64.zip` |
| macOS Intel | `PKHeX-Avalonia-osx-x64.zip` |

All releases are self-contained — no .NET runtime installation required.

---

## Project Structure
* **PKHeX.Avalonia**: The main application (cross-platform).
* **Legacy/PKHeX.WinForms**: The original Windows Forms application, kept as a reference archive.
* **PKHeX.Core**: Shared logic library (synced from [upstream PKHeX](https://github.com/kwsch/PKHeX)).

## Features
* **Save Editing:** Core series save files (.sav, .dsv, .dat, .gci, .bin).
* **Entity Files:** Import and export .pk*, .ck3, .xk3, .pb7, and more.
* **Mystery Gifts:** Support for .pgt, .pcd, .pgf, and .wc* files.
* **Transferring:** Move Pokémon between generations while converting formats automatically.

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

## Versioning

PKHeX Avalonia uses **semantic versioning** (`1.0.0`, `1.1.0`, etc.) for the UI application.
PKHeX.Core follows the upstream date-based versioning scheme.

To create a release, bump the `<UIVersion>` value in `Directory.Build.props` and push to `main`. The release workflow detects the new version, creates the git tag, builds all 4 platforms, and publishes a GitHub release — no manual tagging required.

## Screenshots
*Work in progress — the UI is changing fast.*
<img width="1212" height="790" alt="Screenshot 2026-01-21 at 20 46 16" src="https://github.com/user-attachments/assets/430b2ca2-a011-4d8d-aaa6-f07287e30d6c" />
<img width="1212" height="790" alt="Screenshot 2026-01-21 at 20 46 36" src="https://github.com/user-attachments/assets/1d2d3950-ac98-46bd-853b-c51c1e2e74c3" />
<img width="1212" height="790" alt="Screenshot 2026-01-21 at 20 46 48" src="https://github.com/user-attachments/assets/40d58fc3-86c7-4d3b-bccd-b6c82fecd14a" />
<img width="1212" height="790" alt="Screenshot 2026-01-21 at 20 47 06" src="https://github.com/user-attachments/assets/8d0a1b76-ded5-4119-a079-33a2b08ebf7c" />
<img width="1100" height="677" alt="Screenshot 2026-01-21 at 20 47 32" src="https://github.com/user-attachments/assets/0b9a811c-5fb5-44cc-9f06-5a4dadf6e043" />

## Credits
This fork is built on the incredible work of the [PKHeX team](https://github.com/kwsch/PKHeX).

* **Logic & Research:** [PKHeX](https://github.com/kwsch/PKHeX)
* **QR Codes:** [QRCoder](https://github.com/codebude/QRCoder) (MIT)
* **Sprites:** [pokesprite](https://github.com/msikma/pokesprite) (MIT)
* **Arceus Sprites:** National Pokédex - Icon Dex project and contributors.
