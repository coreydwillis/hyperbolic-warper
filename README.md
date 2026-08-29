# Hyperbolic Warper

A lightweight Windows 11 (WinUI 3) app for time-shifting `.srt` subtitle files: shift every timecode forward or backward, or set the first cue's timecode and let the rest of the file follow, individually or across a whole batch.

## License and AI-assisted development

Hyperbolic Warper is released into the public domain under [The Unlicense](LICENSE). It's free and unencumbered software -- use, modify, fork, and redistribute it however you want, including for commercial purposes, with no attribution required.

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

The app is configured as **unpackaged** (no MSIX/store identity required), so it can be published as a self-contained folder (everything, including the Windows App SDK runtime, is bundled — no separate install step):

```bash
dotnet publish src/HyperbolicWarper.App/HyperbolicWarper.App.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -p:PublishTrimmed=false -p:PublishReadyToRun=false
```

The output lands under `src/HyperbolicWarper.App/bin/x64/Release/net9.0-windows10.0.19041.0/win-x64/publish/`. Zip that folder and distribute it; users unzip and run `HyperbolicWarper.App.exe`.

**Do not add `-p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true`.** That was tried for a more convenient one-file download, but it reproducibly crashes on launch (`0xC000027B` in `Microsoft.UI.Xaml.dll`) on real hardware, every time, while the exact same publish without those two flags launches fine — confirmed by publishing both ways back to back on the same machine. Something about how `PublishSingleFile` extracts native DLLs (including `Microsoft.UI.Xaml.dll`) to a temp directory on first run is incompatible with WinUI 3. This cost a lot of time chasing red herrings (trimming, NuGet resolution, a Windows Server/GPU theory) because it was only ever validated in CI after the single-file switch — it was never actually a CI-only issue.

`-p:PublishTrimmed=false -p:PublishReadyToRun=false` are still required: self-contained publish trims the output by default, which strips managed types that are only reachable through XAML reflection (value converters instantiated via `StaticResource`, never `new`'d from C#) and crashes the app at runtime the first time such a converter runs. See the comment in [`HyperbolicWarper.App.csproj`](src/HyperbolicWarper.App/HyperbolicWarper.App.csproj) for why this can't just be set once in the project file.

`HyperbolicWarper.App/packages.lock.json` and `HyperbolicWarper.Core/packages.lock.json` pin the full resolved dependency graph (not just the top-level package versions above) and are committed to the repo. Restore uses them automatically; if you ever need to change a package version, update the `PackageReference` and re-run a restore or publish to regenerate the lock file, then commit it alongside your change. Don't delete them or restores may silently resolve a different transitive graph than what's been tested.

## Releases

Releases are built and published locally rather than through GitHub Actions — there's no CI step here worth the complexity now that publishing is a single local command. Cutting a release means: bump `<Version>` in [`HyperbolicWarper.App.csproj`](src/HyperbolicWarper.App/HyperbolicWarper.App.csproj), run the publish command above on a real machine, verify the exe launches, zip the publish folder as `HyperbolicWarper-<version>-win-x64.zip`, then create the GitHub Release and attach it manually (tag it `v<version>`).

