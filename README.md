# Hyperbolic Warper

A lightweight Windows 11 (WinUI 3) app for time-shifting `.srt` subtitle files: shift every timecode forward or backward, or set the first cue's timecode and let the rest of the file follow, individually or across a whole batch.

## License and AI-assisted development

Hyperbolic Warper is open source under the [MIT License](LICENSE). You're free to use, modify, fork, and redistribute it, including for commercial purposes, as long as you keep the original copyright notice.

This codebase was built with the assistance of AI coding tools (Anthropic's Claude Code), which were used throughout its design, implementation, and testing. This is disclosed here for anyone who prefers to avoid software built with AI assistance.

## Features

- **Relative shift**: enter Hours / Minutes / Seconds / Milliseconds and a direction; every timecode in the file moves by that amount.
- **Set first timecode**: enter the new start time for the first subtitle, and the app computes the delta and applies it uniformly to every entry.
- **Batch processing**: drop in multiple `.srt` files (or use *Add files*) and process them all at once, using multiple CPU cores.
- **Same or per-file shift**: one checkbox toggles between applying a single shift to every file, or giving each file its own shift/target controls.
- **Negative-timecode clamping**: a shift that would push a timecode below `00:00:00,000` clamps to zero instead of going negative, and is called out in the verification summary.
- **Overwrite or new file**: write to `name.shifted.srt` next to the original, or overwrite the original in place.
- **Post-process verification**: after each file is processed, a summary (and expandable detail panel) shows the requested vs. applied delta, first/last entry before and after, entry counts, and any clamped/ordering issues, so you can sanity-check the result at a glance.

## Solution layout

```
HyperbolicWarper.sln
src/
  HyperbolicWarper.Core/     Plain .NET 9 class library: SRT parsing/writing, shift math, file orchestration. No UI dependency, fully unit tested.
  HyperbolicWarper.App/      WinUI 3 (Windows App SDK) desktop app, unpackaged, MVVM (CommunityToolkit.Mvvm).
tests/
  HyperbolicWarper.Core.Tests/  xUnit tests for the Core library.
```

## Building & running

Requires the .NET 9 SDK and the Windows App SDK / WinUI 3 workload (Visual Studio's ".NET Desktop Development" + "Windows App SDK" components, or the `winui` `dotnet new` templates).

```bash
# Run the test suite
dotnet test tests/HyperbolicWarper.Core.Tests/HyperbolicWarper.Core.Tests.csproj

# Build everything
dotnet build HyperbolicWarper.sln -p:Platform=x64

# Run the app
dotnet run --project src/HyperbolicWarper.App/HyperbolicWarper.App.csproj -p:Platform=x64
```

`Platform` must be one of `x64`, `x86`, or `ARM64` (match your machine).

## Publishing a standalone build

The app is configured as **unpackaged** (no MSIX/store identity required), so it can be published as a self-contained folder and just run:

```bash
dotnet publish src/HyperbolicWarper.App/HyperbolicWarper.App.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true
```

The output lands under `src/HyperbolicWarper.App/bin/x64/Release/net9.0-windows10.0.19041.0/win-x64/publish/`. Copy that folder anywhere and run `HyperbolicWarper.App.exe`.

## Releases

[`.github/workflows/release.yml`](.github/workflows/release.yml) builds a self-contained `win-x64` release and publishes it as a GitHub Release. It only runs when the `<Version>` in [`HyperbolicWarper.App.csproj`](src/HyperbolicWarper.App/HyperbolicWarper.App.csproj) actually changes on `main`, so ordinary pushes don't trigger a build. To cut a release, bump that version and push (or merge a PR that does); the workflow tags the commit `v<version>` and attaches the build as a release asset. It can also be run manually from the Actions tab.

