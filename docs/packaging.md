# Packaging & distribution

This document covers how `release.yml` builds installers for each platform,
which secrets unlock real code signing / notarization, and how to submit the
package-manager templates under `packaging/` once signed builds exist.

Everything described here runs automatically on every release (a push to
`main` that changes `Directory.Build.props`'s `<UIVersion>`, or a manual
`workflow_dispatch`). There are no manual signing steps — the workflow signs
when it can and clearly labels artifacts as unsigned when it can't.

## Artifact matrix

| Platform | Artifact(s) | Notes |
|---|---|---|
| Windows x64 | `PKHeX-Avalonia-win-x64.zip` (unchanged) + `PKHeX-Avalonia-Setup.exe` (or `PKHeX-Avalonia-Setup-unsigned.exe`) | Installer built with Inno Setup via chocolatey |
| Linux x64 | `PKHeX-Avalonia-linux-x64.zip` (unchanged) + `PKHeX-Avalonia-linux-x64.AppImage` (unchanged) | See "Why AppImage, not Flatpak" below |
| macOS arm64 / x64 | `PKHeX-Avalonia-osx-{arm64,x64}.zip` (unchanged, ad-hoc signed as before) + `PKHeX-Avalonia-osx-{arm64,x64}.dmg` or `-unsigned.dmg` | `.dmg` contains the `.app` bundle plus an `Applications` symlink |

The existing `.zip` and `.AppImage` artifacts are unchanged — this is
additive, per the acceptance criteria.

## macOS: signing & notarization

`release.yml`'s `build` job (macOS legs of the matrix) gates real Developer
ID signing on three secrets:

| Secret | Contents |
|---|---|
| `MACOS_CERT_P12` | Base64-encoded `.p12` export of a **Developer ID Application** certificate (`base64 -i cert.p12 \| pbcopy`) |
| `MACOS_CERT_PASSWORD` | Password used when exporting the `.p12` |
| `MACOS_SIGN_IDENTITY` | The identity string codesign should use, e.g. `Developer ID Application: Your Name (TEAMID)` |
| `APPLE_NOTARY_KEY_ID` | Key ID of an App Store Connect API key with the Developer role |
| `APPLE_NOTARY_KEY` | Base64-encoded `.p8` private key for that API key |
| `APPLE_NOTARY_ISSUER_ID` | Issuer ID (UUID) for the API key, from App Store Connect > Users and Access > Keys |

If `MACOS_CERT_P12`, `MACOS_SIGN_IDENTITY`, and `APPLE_NOTARY_KEY` are all
present, the workflow:

1. Imports the certificate into a temporary keychain.
2. Re-signs the `.app` with `codesign --options runtime` (hardened runtime,
   required for notarization) using the Developer ID identity.
3. Submits it to `notarytool`, waits for the result, and staples the ticket
   with `stapler staple`.
4. Packs the notarized `.app` into `PKHeX-Avalonia-osx-<arch>.dmg`.

If any of those secrets are absent, the workflow skips straight to step 4
and names the output `PKHeX-Avalonia-osx-<arch>-unsigned.dmg` so it's obvious
from the filename (and should be called out in release notes) that
Gatekeeper will still complain.

The existing ad-hoc-signed `.zip` artifacts are untouched by this change.

## Windows: installer & code signing

`release.yml`'s Windows leg installs Inno Setup via `choco install
innosetup`, then builds `packaging/windows/installer.iss` into
`PKHeX-Avalonia-Setup.exe` (registers Start Menu / desktop shortcuts and an
Add/Remove Programs entry that uninstalls cleanly).

Code signing (via `signtool.exe`, part of the Windows SDK already present on
`windows-latest`) is gated on:

| Secret | Contents |
|---|---|
| `WINDOWS_CERT_P12` | Base64-encoded `.pfx`/`.p12` of an OV (or EV) code-signing certificate |
| `WINDOWS_CERT_PASSWORD` | Password for that `.pfx` |

If both secrets are present, the workflow signs both the published
`PKHeX.Avalonia.exe` (before packaging) and the final
`PKHeX-Avalonia-Setup.exe` with a SHA-256 signature and a DigiCert RFC 3161
timestamp. If they're absent, the installer is renamed to
`PKHeX-Avalonia-Setup-unsigned.exe` so SmartScreen's "unknown publisher"
warning is expected and self-explanatory from the filename.

**Note:** an OV certificate alone does not eliminate SmartScreen warnings
immediately — Microsoft's reputation system needs download volume to build
trust for a given cert. An EV certificate (or Azure Trusted Signing) avoids
the warning from day one. Either kind of certificate works with the signing
steps above; only the secret contents change.

## Linux: why AppImage, not Flatpak/Flathub

The existing `.AppImage` build (via `appimagetool`) is kept as-is, per the
issue's scope ("AppImage stays as-is"). Flathub distribution is **not**
implemented here:

- Flathub requires a manifest-driven build from source inside a Flatpak
  sandbox (no bundling a self-contained `dotnet publish` output directly),
  plus a review/approval process on their side — this is a separate,
  larger effort than an additive CI step, and the issue explicitly scopes
  Flathub as a stretch goal alongside Homebrew/winget rather than a hard
  CI requirement.
- AppImage requires no external approval and works today, so it remains the
  primary Linux distribution channel until a Flatpak manifest is built as
  follow-up work.

## Package managers (Homebrew cask, winget)

Publishing to Homebrew/winget means opening a PR against **their**
repositories (`homebrew/homebrew-cask`, `microsoft/winget-pkgs`) — this repo
cannot and does not auto-publish there. `packaging/` contains ready-to-fill
templates plus the exact submission steps:

### Homebrew cask — `packaging/homebrew/pkhex-avalonia.rb`

Prerequisite: a release with signed & notarized `.dmg` files (see above) —
Homebrew cask maintainers reject casks whose binaries fail Gatekeeper.

1. `shasum -a 256 PKHeX-Avalonia-osx-arm64.dmg PKHeX-Avalonia-osx-x64.dmg`
   and fill in `version` + both `sha256` values in the template.
2. `brew bump-cask-pr --cask pkhex-avalonia --version <version>` (once the
   cask already exists upstream), or for the first submission, fork
   `homebrew/homebrew-cask`, copy the file to
   `Casks/p/pkhex-avalonia.rb`, and open a PR.
3. `brew audit --cask --online pkhex-avalonia` locally before submitting.

### winget — `packaging/winget/realgarit.PKHeXAvalonia.*.yaml`

Prerequisite: a release with a signed `PKHeX-Avalonia-Setup.exe` — winget
also flags unsigned installers during validation and Microsoft's manual
review is far more likely to reject them.

1. Replace `{{VERSION}}` in all three files and `{{INSTALLER_SHA256}}` in
   the installer manifest (`Get-FileHash PKHeX-Avalonia-Setup.exe -Algorithm
   SHA256` on Windows, or `sha256sum` elsewhere).
2. Easiest path: `wingetcreate update realgarit.PKHeXAvalonia -u
   https://github.com/realgarit/PKHeX-Avalonia/releases/download/v<version>/PKHeX-Avalonia-Setup.exe
   -v <version> -s` (the `-s` submits a PR directly if you're
   authenticated with `gh`).
3. Manual path: `winget validate --manifest packaging/winget/` then copy the
   three files into a fork of `microsoft/winget-pkgs` under
   `manifests/r/realgarit/PKHeXAvalonia/<version>/` and open a PR.

## Summary: what's automatic vs. gated vs. manual

- **Fully automatic, every release:** zip artifacts (all platforms),
  AppImage, `.dmg` (signed or unsigned), Windows installer (signed or
  unsigned), GitHub Release creation and asset upload.
- **Gated on secrets (automatic once configured):** Developer ID codesigning
  + notarization/stapling for macOS, code signing for the Windows installer
  and exe.
- **Manual, one-time-per-version, by design (cannot be automated without
  publishing into third-party repos on the maintainer's behalf):**
  submitting the Homebrew cask and winget manifest PRs. Flathub packaging is
  left as future work.
