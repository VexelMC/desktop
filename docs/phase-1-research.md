# Phase 1 — McPatch research and Vexel architecture

Date: 2026-08-26

## Scope and conclusion

This phase is research only. No Vexel patch implementation or Minecraft-build
compatibility claim has been made.

The McPatch source reviewed was commit `75ca2a91a1bb50ee7cf32142995eea27b6adb200`
on `Zwuiix-cmd/McPatch`, together with the current `sdk` branch definitions at
`ef2b6a799a21eda45e03ae963736b6bd6c5bd861`.

McPatch is useful historical evidence of the kinds of locations its authors
patched. It is not a safe compatibility source for Vexel. Its README also
states that it is discontinued and works only before 1.21.114.

## What McPatch does

### Game discovery

- It starts `minecraft://` if `Minecraft.Windows.exe` is not already running.
- It uses ToolHelp process enumeration to find a process named exactly
  `Minecraft.Windows.exe`.
- It calls `GetModuleFileNameExA` on that running process to obtain its module
  path.
- It reads a package name under the current user's AppModel Repository registry
  key and extracts the version-shaped component after `Microsoft.MinecraftUWP_`.
- It then terminates the game before overwriting the executable in place.

`main.cpp` additionally contains `FindPackagesByPackageFamily` and
`GetPackagePathByFullName` helpers, but they are not used for the normal path.

### Definitions and parsing

- `SDK::parseSDK` downloads `sdk.json` from the repository's `sdk` branch.
- `SDK::parsePatches` downloads a separate map of replacement-byte sequences.
- Signatures are strings such as `E8 ? ? ? ? 48 8B 03` and replacements are
  space-separated hexadecimal bytes.
- `parseSignature` converts `?` to `-1`; all other two-character tokens become
  byte values.

### Scanning and patching

- The entire executable is loaded into memory.
- `findSignature` uses a naive forward scan and returns the first matching
  location.
- Each non-wildcard replacement byte overwrites data starting at the beginning
  of that signature.
- The original complete executable is copied to
  `Minecraft.Windows.<version>.exe` before any patch loop runs.
- The altered bytes are written back to the same executable file.
- `PlayScreenFix` is a separate whole-file string replacement: every occurrence
  of `mc-ab-new-play-screen-` is replaced with spaces.

## McPatch weaknesses that Vexel must not inherit

1. **No build identity.** The selected signature is not keyed by SHA-256,
   PE metadata, architecture, or a verified version family.
2. **First match wins.** Zero, one, and many matches are not distinguished.
   An ambiguous match can silently patch unrelated code.
3. **No target validation.** It scans all file bytes rather than an expected PE
   section, checks no instruction context, and does not compare expected
   original bytes before replacement.
4. **No explicit patch offset.** Replacement begins at signature offset zero,
   which makes each pattern and modification tightly coupled and hard to audit.
5. **Unsafe remote data.** Definitions are fetched over HTTPS, but there is no
   schema version, signature, hash pinning, timeout policy, cache, or safe
   fallback. Failed HTTP responses and malformed JSON can propagate as errors.
6. **Weak restoration model.** A copied executable is not an address-level
   restoration record. It is overwritten on each run, does not verify its own
   hash, and cannot selectively restore a feature.
7. **Risky process flow.** The program launches Minecraft, busy-waits until it
   starts, requests excessive process access, then terminates it. It has no
   cancellation, timeout, or user-controlled lifecycle.
8. **No runtime session.** File edits and process APIs are mixed, but runtime
   patch state, process identity, original bytes, and cleanup are not modeled.
9. **No error contract or tests.** A missing signature logs a message, but the
   operation can still be reported generally as complete. There are no unit
   tests or synthetic fixtures.
10. **Mutable string patching.** Replacing every matching resource string in a
    binary is especially unsafe without a build-specific expected match count.

The parser's positional format also assumes a particular spacing layout and
does not give useful diagnostics for malformed or odd-length tokens. The naive
scanner can underflow for a signature longer than the input.

## Concepts Vexel can reuse

- Wildcard byte patterns are a reasonable *candidate-location* mechanism.
- Separating declarative definitions from patch execution is the right
  direction.
- Process discovery and Microsoft Store package APIs are relevant, but need a
  layered detection strategy and reliable error handling.
- Keeping original bytes before a modification is essential, but it must be
  feature- and session-specific.

Everything else should be redesigned for data validation, exact build matching,
recoverability, and a clear status model.

## Feature research status

McPatch's current remote definitions name `GuiScale`, `TeleportRotation`,
`ItemUseDelay`, `MinimalViewBobbing`, `NoHurtCam`, `NoJumpDelay`,
`PlayScreenFix`, and `ThirdPersonNametag`. Some use a `NOP` replacement, while
`GuiScale` and `NoJumpDelay` replace values/instructions.

This only identifies historical implementation candidates. It does **not**
establish that a signature maps to the stated behaviour on any current or
legacy Minecraft executable. Vexel currently treats every Minecraft build and
every requested patch as **unverified/unsupported** until reverse-engineered
and tested against that exact executable fingerprint.

`AutoSprint` must be researched separately. It must not be implemented as a
static-byte placeholder or any method intended to bypass server-side movement
validation.

## Proposed Vexel solution architecture

```text
Vexel.sln
src/
  Vexel.App/                 WPF shell, MVVM views and view models
  Vexel.Application/         use cases, state orchestration, DI composition
  Vexel.Core/                domain models, contracts, validation results
  Vexel.Compatibility/       schema, manifest validation, resolver, cache
  Vexel.Patching/            patterns, PE section scanner, verifier, sessions
  Vexel.Platform.Windows/    packages, processes, PE metadata, Win32 memory I/O
tests/
  Vexel.Core.Tests/
  Vexel.Compatibility.Tests/
  Vexel.Patching.Tests/
compatibility/
  bundled/
    manifest.json
    families/
docs/
  phase-1-research.md
  verification-records/
```

### Core domain contracts

`IPatch` is a version-independent capability contract:

```text
Id, Name, Description
DetectAsync(build, process)
ApplyAsync(session)
RestoreAsync(session)
VerifyAsync(session)
```

Patch definitions remain declarative. A definition includes its patch ID,
supported architecture, executable fingerprint selector, version range for
display only, allowed PE sections, search pattern, expected match count, patch
offset, expected original bytes, replacement bytes, and instruction-context
rules. A replacement cannot be applied unless all validators approve it.

### Build identity and compatibility resolution

`MinecraftBuildFingerprint` contains package identity/path, file and product
versions, file size, PE timestamp, machine architecture, and SHA-256. The
compatibility resolver selects only a definition explicitly matching the
fingerprint. Version labels are informative; a SHA-256/fingerprint record is
the authority for a verified patch.

Definitions are organized by compatible implementation family without
duplicating them, but a family is only introduced after binary analysis proves
the grouping. Definitions are classified as `verified`, `experimental`, or
`unsupported`; the UI only enables verified entries by default.

### Safe patch pipeline

1. Discover the installed package and, independently, the active process.
2. Fingerprint the selected executable; resolve a compatible definition.
3. Parse and inspect PE sections; scan only the definition's allowed section.
4. Require the exact declared match count.
5. Verify surrounding bytes and x64 instruction context with Iced.
6. Read and compare the declared original target bytes.
7. Apply only the declared replacement through the selected strategy.
8. Read back and verify the result; record an auditable patch session.

Any failed gate leaves the executable/process untouched and produces a specific
status such as `Unsupported`, `Ambiguous signature`, or `Signature mismatch`.

### Patch strategies and state

The patch engine has separate `RuntimeMemoryPatchStrategy`,
`BackedUpFilePatchStrategy`, and `SettingsPatchStrategy` implementations.
Feature modules select the least invasive strategy proven to work. A runtime
`PatchSession` records PID, process start identity, module base, exact address,
original and replacement bytes, status, and timestamps. Memory addresses are
never reused after process exit.

If a disk patch is ever justified, Vexel creates a timestamped backup and a
metadata record containing original and modified SHA-256 values before writing.
Restoration verifies the recorded target before changing anything. Backups are
never removed by reset unless the user explicitly asks.

### Compatibility updates

The remote client obtains a signed manifest over HTTPS with a short timeout.
It validates schema version, detached signature, file hashes, allowed IDs, and
all patch constraints before atomically caching the data. It never downloads
code, DLLs, or scripts. Invalid/offline remote data falls back to the bundled
database without interrupting local detection.

### Product behaviour

The WPF application uses MVVM and asynchronous commands so scanning does not
block the interface. Preferences are stored separately from observed runtime
state. For example, an enabled preference on a changed Minecraft build is shown
as `Preference: enabled; Runtime: unsupported`, never as active.

Structured logs contain operation IDs and safe diagnostic metadata only.
`Copy diagnostics` reports detection and validation outcomes without paths or
other sensitive data unless the user deliberately includes them.

## Planned delivery sequence

1. Project foundation: .NET 8 WPF solution, MVVM, dependency injection,
   settings, logging, UI shell, CI, and basic tests.
2. Minecraft discovery and fingerprinting, with synthetic PE/file tests.
3. Pattern parser/scanner and PE-aware validation tests.
4. Compatibility schema, signed-manifest/cache behaviour, and resolver tests.
5. Patch-session engine with apply/restore/read-back tests on synthetic data.
6. Reverse-engineer and prove one feature (preferably No Hurt Cam) on one exact
   Minecraft build before any feature is surfaced as available.
7. Add independently verified feature/build records one at a time, then polish
   the UI and release packaging.

## Phase 1 exit criteria

- Historical source mechanics and limits are documented.
- The target architecture and safety gates are defined.
- No unverified signature is included as support data.
- No Minecraft binary is modified.
