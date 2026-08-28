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

The app is configured as **unpackaged** (no MSIX/store identity required), so it can be published as a single, self-contained `.exe` (everything, including the Windows App SDK runtime, is bundled into the file and extracted to a temp folder on first launch):

```bash
dotnet publish src/HyperbolicWarper.App/HyperbolicWarper.App.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:WindowsAppSDKSelfContained=true -p:PublishTrimmed=false -p:PublishReadyToRun=false
```

The output lands under `src/HyperbolicWarper.App/bin/x64/Release/net9.0-windows10.0.19041.0/win-x64/publish/HyperbolicWarper.App.exe`. Copy that file anywhere and run it.

`-p:PublishTrimmed=false -p:PublishReadyToRun=false` are required, not optional: self-contained publish trims the output by default, which strips managed types that are only reachable through XAML reflection (value converters instantiated via `StaticResource`, never `new`'d from C#) and crashes the app at runtime the first time such a converter runs. See the comment in [`HyperbolicWarper.App.csproj`](src/HyperbolicWarper.App/HyperbolicWarper.App.csproj) for why this can't just be set once in the project file.

`HyperbolicWarper.App/packages.lock.json` and `HyperbolicWarper.Core/packages.lock.json` pin the full resolved dependency graph (not just the top-level package versions above) and are committed to the repo. Restore uses them automatically; if you ever need to change a package version, update the `PackageReference` and re-run a restore or publish to regenerate the lock file, then commit it alongside your change. Don't delete them or restores may silently resolve a different transitive graph than what's been tested.

## Releases

Releases are built and published locally rather than through GitHub Actions. GitHub's standard hosted Windows runners have no GPU, and WinUI 3 has a documented failure creating its `Window` with no hardware rendering device present (`0x8898008D`, matching [microsoft/microsoft-ui-xaml#10626](https://github.com/microsoft/microsoft-ui-xaml/issues/10626) and [#8446](https://github.com/microsoft/microsoft-ui-xaml/issues/8446)) — every CI-built release crashed in exactly that way despite working fine on real hardware, so there was no reliable way to validate a release build in that environment. Cutting a release means: bump `<Version>` in [`HyperbolicWarper.App.csproj`](src/HyperbolicWarper.App/HyperbolicWarper.App.csproj), run the publish command above on a real machine, verify the exe launches, then create the GitHub Release and attach `HyperbolicWarper-<version>-win-x64.exe` manually (tag it `v<version>`).

