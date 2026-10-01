---
name: guishark-build-run
description: Build, compile, validate, and launch GuiShark SDK projects and demos from this repository. Use when working with its dotnet commands, demo startup, or local build checks.
---

# Build and run GuiShark

Run commands from the repository root containing `Gui.Shark.sln`. On this Windows checkout:

```powershell
cd C:\Work\GuiShark
```

Use the actual checkout location on other machines. Install a stable .NET 10 SDK compatible with `global.json`. Demos need a desktop display and an OpenGL 3.3 core driver; the regression tests need no GL context. Shaping cases load the Skia/HarfBuzz native libraries restored with the test project. Solution coverage verification additionally needs Git and PowerShell 7 (`pwsh`).

## Build / compile

Build the complete solution once before launching demos:

```powershell
dotnet build Gui.Shark.sln
```

This restores dependencies and compiles both SDK libraries, all four demos, and the regression tests in Debug. For an individual project, use its project path:

```powershell
dotnet build src/GuiShark/GuiShark.csproj -c Debug
dotnet build src/GuiShark.OpenGL/GuiShark.OpenGL.csproj -c Debug
```

For Release:

```powershell
dotnet build Gui.Shark.sln -c Release
```

Ordinary builds already run analyzers. Follow [CONTRIBUTING.md](../../../CONTRIBUTING.md) for enforced rules; keep complexity thresholds and security checks intact when resolving failures.

## Run demos

After the Debug solution build, choose one command:

```powershell
# HTML/CSS playground
dotnet run --no-build --project src/demos/GuiShark.Demo

# Lantern Valley: balloon game with an HTML/CSS HUD
dotnet run --no-build --project src/demos/GuiShark.Balloon

# Text Lab: compare Skia with and without HarfBuzz shaping
dotnet run --no-build --project src/demos/GuiShark.TextDemo

# Controls Gallery: tabs, passwords, dialogs, scrolling, and native IME input
dotnet run --no-build --project src/demos/GuiShark.ControlsDemo

# Multilingual page and Arabic shaping comparison
dotnet run --no-build --project src/demos/GuiShark.ControlsDemo -- --page=multilingual
dotnet run --no-build --project src/demos/GuiShark.TextDemo -- --sample arabic
```

Close the window before running another command in the same terminal. To use Release binaries, add `-c Release` to `dotnet run` after building Release. `--no-build` uses existing binaries; rebuild after C# changes, or omit that flag to build as part of launching.

For source HTML/CSS editing with F5 reload in the playground or controls gallery, pass the source asset directory after `--`:

```powershell
dotnet run --no-build --project src/demos/GuiShark.Demo -- src/demos/GuiShark.Demo/Assets
dotnet run --no-build --project src/demos/GuiShark.ControlsDemo -- src/demos/GuiShark.ControlsDemo/Assets
```

Without this argument, demos use assets copied to the build output. Detailed controls and options are in [README.md](../../../README.md) and the demo guides linked there.

## Validate changes

Use the same full solution coverage, fresh builds, and test commands as CI:

```powershell
pwsh -File eng/Verify-SolutionCoverage.ps1
dotnet build Gui.Shark.sln -c Debug --no-incremental -v minimal
dotnet build Gui.Shark.sln -c Release --no-incremental -v minimal
dotnet test --solution Gui.Shark.sln -c Debug --no-build --no-restore
dotnet test --solution Gui.Shark.sln -c Release --no-build --no-restore
git diff --check
```

The coverage script checks tracked C# projects; stage new project files and include them in `Gui.Shark.sln` before using it. `global.json` selects Microsoft Testing Platform, so retain the `--solution` syntax for tests. Report actual build/test outcomes and any missing SDK, driver, or display dependency; do not claim a demo was verified merely because it compiled.
