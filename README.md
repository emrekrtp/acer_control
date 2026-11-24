# Acer Kontrol

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![CI](https://github.com/emrekrtp/acer_booster/actions/workflows/ci.yml/badge.svg)](https://github.com/emrekrtp/acer_booster/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/emrekrtp/acer_booster?color=ef233c)](https://github.com/emrekrtp/acer_booster/releases)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6.svg)](https://www.microsoft.com/windows)

A lightweight, modern dark-themed Windows control center and system tray utility for **Acer Nitro 5 (AN515-58)** that brings the core features of **NitroSense** and **Acer Care Center** into a single fast, open-source application.

## Features

- **Live Overview Dashboard:** Real-time CPU/GPU temperatures (with thermal color indicators), CPU/RAM usage, CPU/GPU fan RPM, battery health & status, and one-click quick controls.
- **Fan & Performance Control:**
  - Performance profiles: **Quiet**, **Balanced**, **Performance**, and **Turbo**.
  - Fan modes: **Auto**, **Max**, and **Custom** (0–100% independent or synchronized CPU/GPU fan speed sliders).
  - **CoolBoost** toggle and quick **Windows Power Plan** selector (Power Saver / Balanced / High Performance).
- **4-Zone RGB Keyboard Lighting:**
  - Interactive 4-zone live keyboard preview with per-zone or unified color picker.
  - Lighting effects: **Static**, **Breathing**, **Neon**, **Wave**, **Shifting**, and **Zoom**.
  - Brightness (0–100%), animation speed (1–9), direction (Left-to-Right / Right-to-Left), and one-click presets.
- **Battery Charge Limit (80%) & System Tools:**
  - Automatically enforces the 80% battery charge limit every minute, after wake from sleep, and on AC plug/unplug events.
  - Live **Display Brightness** slider, **LCD Overdrive**, **Power-Off USB Charging**, 30-second keyboard backlight timeout, and automatic reapply on wake.
- **Rich System Tray:**
  - Left-click to show/hide the control panel (near-zero background resource usage while hidden).
  - Right-click context menu for instant access to battery charge limit, fan modes, performance profiles, and RGB presets.

## Download (Prebuilt EXE)

Grab the latest release from the [Releases](https://github.com/emrekrtp/acer_booster/releases) page:

- **`AcerKontrol-vX.Y.Z-win-x64.exe`** (or `...-standalone.zip`): Single-file standalone executable — **no .NET installation required**.
- **`AcerKontrol-vX.Y.Z-win-x64-portable.zip`**: Compact single-file package (~785 KB) for systems that already have the .NET 9 Desktop Runtime installed.

## Requirements

- **OS:** Windows 10 or Windows 11 (64-bit)
- **Privileges:** **Administrator** privileges are required to communicate with the Acer WMI hardware interface.
- **Hardware:** Developed and tested on **Acer Nitro 5 (AN515-58)**. Other Acer Nitro / Predator models sharing the same Acer WMI interface may also work partially or fully.
- **Building from source:** [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Install / Uninstall from Source

Run the PowerShell scripts below (automatically prompts for Administrator elevation if needed):

```powershell
pwsh scripts\install.ps1     # builds, installs to Program Files, and registers logon autostart
pwsh scripts\uninstall.ps1   # stops the app and removes installation & scheduled task
```

- **Install directory:** `%ProgramFiles%\AcerKontrol`
- **Settings file:** `%APPDATA%\AcerKontrol\settings.json`

## Development & Testing

```powershell
dotnet build
dotnet test
```

To build local single-file release artifacts into `publish\release\`:

```powershell
pwsh scripts\build-release.ps1
```

### Project Structure

```text
acer_booster/
├── src/AcerKontrol/         # Main Windows Forms app (WMI, fan, RGB, battery, tray, modern UI)
├── tests/AcerKontrol.Tests/ # Unit tests (codecs, battery, settings, status formatting)
├── scripts/                 # PowerShell install, uninstall, and local release scripts
└── docs/                    # Architecture and design documents
```

## Contributing

Bug reports, model compatibility feedback, and Pull Requests are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## Disclaimer

This project is **not an official Acer product** and is not affiliated with or endorsed by Acer Inc. It interacts directly with hardware settings (fan speeds, power profiles, battery charge limits, etc.) via WMI; use at your own risk.

## License

Licensed under the [MIT License](LICENSE).
