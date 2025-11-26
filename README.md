# IAOSB NUMTAL — AGVDesktop

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/YOUR_GH_USERNAME/AGVDesktop/actions)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

A modern, polished WPF desktop application with Windows 11 Fluent styling, Mica/Acrylic backdrop support, runtime-generated application icon, minimize-to-tray, persistent settings and logging, and telemetry visualization — packaged and ready for distribution.

**Table of Contents**
- **Overview**
- **Highlights**
- **Screenshots**
- **Requirements**
- **Quick Start**
- **Settings & Logs**
- **Packaging & Releases**
- **Continuous Integration**
- **Contributing**
- **License**
- **Authors & Support**

**Overview**
- Compact, well-instrumented WPF application focused on a modern UX for Windows 11 while retaining graceful fallbacks for older Windows versions.
- Implements defensive Mica enablement via DWM; when unavailable, it falls back to Acrylic and attempts FluentWPF host-backdrop APIs reflectively.
- Designed for maintainability: small MVVM surface (`ViewModels/MainViewModel.cs`), lightweight services (`Services/SettingsService.cs`, `Services/UiLogService.cs`), and clear interop points.

**Highlights**
- **Modern look**: titlebar with rounded corners, entrance animations, hover effects and an acrylic/mica-like backdrop.
- **System Mica**: attempts to enable Windows 11 Mica using DWM attributes when available.
- **Fluent fallback**: reflection-based attempt to use `FluentWPF` host/backdrop APIs if system Mica is not available.
- **Runtime icon**: generates a crisp application icon at runtime and uses it for both the window and the tray.
- **Minimize to tray**: user-configurable; runs from the system tray using a `NotifyIcon` with context menu.
- **Publish artifacts**: `publish/` and `publish\\win-x64/` produced during release; zips: `AGVDesktop-publish.zip` and `AGVDesktop-win-x64-selfcontained.zip`.

**Screenshots**
- The `MainWindow` contains controls for telemetry, diagnostics and settings. (Add screenshots in `docs/screenshots/` and reference them here.)

**Requirements**
- Windows 10/11 (Windows 11 recommended for Mica visual).
- .NET SDK 7.0 (the project targets `net7.0-windows`).
- Optional: `gh` CLI for release automation and `git` for source control.

**Quick Start**
- Clone the repository:

```powershell
cd C:\projects
git clone https://github.com/YOUR_GH_USERNAME/AGVDesktop.git
cd AGVDesktop
```

- Build and run (developer):

```powershell
dotnet restore
dotnet build -c Debug
dotnet run
```

- Create a Release (locally):

```powershell
# Framework-dependent publish
dotnet publish -c Release -o publish
Compress-Archive -Path publish\\* -DestinationPath AGVDesktop-publish.zip -Force

# Self-contained Windows x64 publish
dotnet publish -c Release -r win-x64 --self-contained true -o publish\\win-x64
Compress-Archive -Path publish\\win-x64\\* -DestinationPath AGVDesktop-win-x64-selfcontained.zip -Force
```

**Settings & Logs**
- Diagnostics are exposed in the UI: OS build, DWM HRESULT from Mica attempts, reflection trace from FluentWPF fallback attempts, and paths to the settings/log files.

**Packaging & Releases**
- Prefer publishing zips as GitHub Release assets rather than committing binary artifacts into source control.
- Use `gh` CLI to create releases and upload the published zips:

```powershell
# create a tag and release (example)
git tag -a v1.0.0 -m "Initial release"
git push origin v1.0.0
gh release create v1.0.0 AGVDesktop-publish.zip AGVDesktop-win-x64-selfcontained.zip --title "v1.0.0" --notes "Initial release"
```

**Continuous Integration (recommended)**
- Add a GitHub Actions workflow to build and publish artifacts on push to `main` and on PRs. See `.github/workflows/ci.yml` in this repository for a sample pipeline.
- The CI job should:
	- Restore and build (`dotnet restore`, `dotnet build`)
	- Run unit tests (if added)
	- Publish artifacts and upload them as workflow artifacts or create a release via `gh`.

**Contributing**
- Contributions are welcome — please open a Pull Request against `main`.
- Follow these guidelines:
	- Keep changes small and focused. One issue → one PR.
	- Add tests for new behavior when practical.
	- Update `README.md` and `CHANGELOG.md` (if present) with notable changes.
	- Respect existing code style.

**Code of Conduct**
- Be respectful and constructive. Report issues and suggest improvements via GitHub Issues.

**License**
- This project is released under the `Apache-2.0` license. See `LICENSE` for details.

**Authors & Support**
- Primary maintainer: cryptixas1 — add contact or GitHub handle here.
- For support, open an issue on the repository.

