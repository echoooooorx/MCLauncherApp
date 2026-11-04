# Minecraft Multiplayer Helper (MCLauncherApp)

[中文](README.md) | [English](README_EN.md)

A small Windows desktop tool that folds the whole routine of "start the Minecraft server + open the SakuraFrp tunnel + voice tunnel" — the steps you repeat every time you play with friends — into a single window and a single click.

Built for 1.20.1 Fabric offline multiplayer: the server runs with `online-mode=false`, and friends join through the tunnel address with offline accounts.

> Note: This project was developed with AI assistance — requirements, testing, and acceptance were done by the author in person; the code was written by AI and verified by the author on a real machine.

## Features

- **One-click start/stop**: launches the server process together with the SakuraFrp game tunnel and voice tunnel (Simple Voice Chat), and shuts everything down cleanly
- **Dual log panels**: server log and tunnel log shown separately, with search (Ctrl+F) and a command input that sends commands to the server console (Shift+Enter for multi-line)
- **Expand/collapse animation**: the log area collapses into a compact window with an eased animation
- **Automatic world backup** before the server starts, plus a manual backup manager (view / restore / delete)
- **Status page**: server state, online players, and spark-friendly diagnostic info
- **Settings page**: SakuraFrp access key, game/voice tunnel IDs, server directory, PCL2 path, and server memory parameters (-Xms/-Xmx with Aikar G1GC flags), all stored in a local `config.json`

## Requirements

- Windows 10/11, .NET Framework 4.x (included with Windows)
- A Minecraft Java server folder (default `D:\MCServer`, configurable)
- A [SakuraFrp](https://www.natfrp.com/) account and tunnels (TCP for the game, UDP for voice), plus a standalone `frpc.exe`
- First run: open the Settings page and fill in the access key and the two tunnel IDs; the log area will guide you

## Building

A single-file WinForms program with no third-party dependencies. The C# compiler that ships with Windows is enough:

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe ^
  /win32icon:assets\icon.ico /win32manifest:app.manifest /out:MCLauncherApp.exe ^
  /reference:System.dll /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll ^
  Program.cs
```

(The `assets\icon.ico` icon comes from the author's local deployment and is not included in this repo; just drop the `/win32icon` argument to build without an icon.)

## Configuration

All settings live in `config.json` next to the executable, read and written through the Settings page. **This file contains your SakuraFrp access key — never commit it to any repository.** This repo's `.gitignore` already excludes it.

## Known issues

- Fixed in v6.19: a horizontal stripe flashed across the log area while expanding, and the log flickered while scrolling (see the version note below). The stripe survived three fix rounds in v6.14–v6.17; in v6.19, a slowed-down animation was scanned frame by frame and the root cause turned out to be the window class missing the CS_VREDRAW/CS_HREDRAW styles — on every resize step, only the newly exposed strip waited for a later paint. The fix adds both redraw styles so every step repaints the whole window, plus WS_EX_COMPOSITED so each frame is presented atomically; log appends are batched under WM_SETREDRAW into a single repaint.

## Version

Current version **v6.19**. The single source file `Program.cs` is the complete program.
