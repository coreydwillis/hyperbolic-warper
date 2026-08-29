# Hyperbolic Warper

A lightweight Windows 11 (WinUI 3) app for time-shifting `.srt` subtitle files: shift every timecode forward or backward, or set the first cue's timecode and let the rest of the file follow, individually or across a whole batch.

Built with AI assistance (Claude Code).

## Features

- **Relative shift**: enter Hours / Minutes / Seconds / Milliseconds and a direction; every timecode in the file moves by that amount.
- **Set first timecode**: enter the new start time for the first subtitle, and the app computes the delta and applies it uniformly to every entry.
- **Batch processing**: drag & drop `.srt` files (or use File > Open file(s)...) and process them all at once, using multiple CPU cores.
- **Same or per-file shift**: one checkbox toggles between applying a single shift to every file, or giving each file its own shift/target controls.
- **Negative-timecode clamping**: a shift that would push a timecode below `00:00:00,000` clamps to zero instead of going negative, and is called out in the verification summary.
- **Overwrite or new file**: write to `name.shifted.srt` next to the original, or overwrite the original in place.
- **Post-process verification**: after each file is processed, a summary (and expandable detail panel) shows the requested vs. applied delta, first/last entry before and after, entry counts, and any clamped/ordering issues. "Show in folder" jumps straight to the output file, and the full batch's verification details can be exported as a text log.

## Installation

Download the latest release from the [Releases page](../../releases) and run the `.exe`.

## Building from source

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

### Solution layout

```
HyperbolicWarper.sln
src/
  HyperbolicWarper.Core/     Plain .NET 9 class library: SRT parsing/writing, shift math, file orchestration. No UI dependency, fully unit tested.
  HyperbolicWarper.App/      WinUI 3 (Windows App SDK) desktop app, unpackaged, MVVM (CommunityToolkit.Mvvm).
tests/
  HyperbolicWarper.Core.Tests/  xUnit tests for the Core library.
```

### Publishing a standalone build

The app is configured as **unpackaged** (no MSIX/store identity required), so it can be published as a self-contained folder -- everything, including the Windows App SDK runtime, is bundled in, no separate install step:

```bash
dotnet clean src/HyperbolicWarper.App/HyperbolicWarper.App.csproj -c Release -p:Platform=x64
dotnet publish src/HyperbolicWarper.App/HyperbolicWarper.App.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -p:PublishTrimmed=false -p:PublishReadyToRun=false
```

Always `clean` before publishing a release build -- reusing another configuration's intermediate build output can produce a broken bundle. The output lands under `src/HyperbolicWarper.App/bin/x64/Release/net9.0-windows10.0.19041.0/win-x64/publish/`. Zip that folder for distribution; users unzip and run `HyperbolicWarper.App.exe`.

`-p:PublishTrimmed=false -p:PublishReadyToRun=false` are required: self-contained publish trims the output by default, which strips managed types only reachable through XAML reflection (value converters instantiated via `StaticResource`, never `new`'d from C#) and crashes the app at runtime the first time such a converter runs.

`HyperbolicWarper.App/packages.lock.json` and `HyperbolicWarper.Core/packages.lock.json` pin the full resolved dependency graph and are committed to the repo. If you change a package version, update the `PackageReference` and re-run a restore or publish to regenerate the lock file, then commit it alongside your change.

## License

Public domain, released under [The Unlicense](LICENSE). Use, modify, fork, and redistribute it however you want, including for commercial purposes, with no attribution required.

## Contributing

Issues and pull requests are welcome.
