# Vexel

A modern desktop utility for fixes, tweaks, and quality-of-life improvements
for Minecraft: Bedrock Edition on Windows.

## Project status

Vexel is in pre-alpha development. It can safely discover a Minecraft for
Windows package, detect a running `Minecraft.Windows.exe` process, and create
an executable fingerprint. Its pattern scanner and patch session engine are
covered by synthetic tests.

There are currently **no verified Minecraft patch definitions**. The app will
not claim that a feature is available or modify Minecraft until a patch has
been independently verified against an exact executable fingerprint.

## Features

Planned feature modules:

- Item Delay Fix
- No Camera Reset
- AutoSprint
- No Hurt Cam
- GUI Scale

Each module will report a precise state: available, active, inactive,
unsupported, signature mismatch, or Minecraft not running. A saved preference
is always distinct from an active runtime patch.

## Compatibility

Vexel supports Windows 10 x64 and Windows 11 x64. Minecraft build detection is
version-aware and records file/product version, file size, SHA-256, PE timestamp,
and architecture. A game-version label by itself is never enough to authorize a
patch.

The compatibility database will contain only explicit, tested build records.
Unknown or changed Minecraft executables remain unsupported and untouched.

## Installation

Release builds will be distributed as self-contained Windows x64 artifacts. No
Visual Studio or .NET runtime is required to run a release build.

## Building

Install the .NET 8 SDK, then run:

```powershell
dotnet restore Vexel.sln
dotnet build Vexel.sln
dotnet test Vexel.sln
dotnet publish src/Vexel.App/Vexel.App.csproj -c Release -r win-x64 --self-contained true
```

## Screenshots

The current WPF shell uses a graphite and dark-blue surface with cyan accents.
Screenshots will be added once a verified feature can be demonstrated honestly.

## Disclaimer

Vexel will never apply a patch to an unknown location. Do not use experimental
builds in multiplayer environments. You remain responsible for following
Minecraft's terms and the rules of any server you join.

## Contributing

Contributions should preserve the safety model: add tests, pin every supported
patch to a verified executable fingerprint, validate match counts and original
bytes, and document real-version verification before marking a feature as
supported.
